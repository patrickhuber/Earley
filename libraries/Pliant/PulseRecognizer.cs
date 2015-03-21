﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pliant
{
    public class PulseRecognizer
    {
        public IGrammar Grammar { get; private set; }
        public Chart Chart { get; private set; }
        public int Location { get; private set; }

        public PulseRecognizer(IGrammar grammar)
        {
            Grammar = grammar;
            Initialization();
        }
        
        private void Initialization()
        {
            Location = 0;
            Chart = new Chart();
            foreach (var startProduction in Grammar.StartProductions())
            {
                var startState = new State(startProduction, 0, 0);
                if (Chart.Enqueue(0, startState))
                    Log("Start", 0, startState);
            }

            Reduce();
        }

        public bool Pulse(char token)
        {
            // https://github.com/jeffreykegler/kollos/blob/master/notes/misc/leo2.md
            var earleme = Chart.Earlemes[Location];
            
            RunScans(token);
            
            // Move to next earlmeme
            Location++;

            bool tokenNotRecognized = Chart.Earlemes.Count <= Location;
            if (tokenNotRecognized)
                return false;
            
            Reduce();
            
            return true;
        }
        
        private void RunScans(char token)
        {
            IEarleme earleme = Chart.Earlemes[Location];
            for (int s = 0; s < earleme.Scans.Count; s++)
            {
                var scanState = earleme.Scans[s];
                Scan(scanState, Location, token);
            }
        }

        private void Reduce()
        {
            IEarleme earleme = Chart.Earlemes[Location];
            var resume = true;

            int p = 0;
            int c = 0;
            int t = 0;

            while (resume)
            {
                if (c < earleme.Completions.Count)
                {
                    var completion = earleme.Completions[c];
                    Complete(completion, Location);
                    c++;
                }
                else if (p < earleme.Predictions.Count)
                {
                    var prediction = earleme.Predictions[p];
                    Predict(prediction, Location);
                    p++;
                }
                else if (t < earleme.Transitions.Count)
                {
                    t++;
                }
                else
                    resume = false;
            }
        }

        private void Predict(IState sourceState, int j)
        {
            var nonTerminal = sourceState.CurrentSymbol() as INonTerminal;
            foreach (var production in Grammar.RulesFor(nonTerminal))
            {
                PredictProduction(sourceState, j, production);
            }
        }

        private void PredictProduction(IState sourceState, int j, IProduction production)
        {
            var state = new State(production, 0, j);
            if (Chart.Enqueue(j, state))
                Log("Predict", j, state);

            var stateIsNullable = state.Production.RightHandSide.Count == 0;
            if (stateIsNullable)
            {
                var aycockHorspoolState = new State(sourceState.Production, sourceState.Position + 1, j);
                Chart.Enqueue(j, aycockHorspoolState);
                Log("Predict", j, aycockHorspoolState);
            }
        }

        private void Scan(IState scan, int j, char token)
        {
            int i = scan.Origin;
            for (var s = 0; s < Chart[j].Count; s++)
            {
                var state = Chart[j][s];
                if (state.StateType == StateType.Transitive)
                    continue;
                if (!state.IsComplete())
                {
                    var currentSymbol = state.CurrentSymbol();
                    if (currentSymbol.SymbolType == SymbolType.Terminal)
                    {
                        var terminal = currentSymbol as ITerminal;
                        if (terminal.IsMatch(token))
                        {
                            var scanState = new ScanState(
                                state.Production,
                                state.Position + 1,
                                i,
                                token);
                            if (Chart.Enqueue(j + 1, scanState))
                                LogScan(j + 1, scanState, token);
                        }
                    }
                }
            }
        }
        
        private void Complete(IState completed, int k)
        {
            var earleme = Chart.Earlemes[k];
            var searchSymbol = completed.Production.LeftHandSide;
            OptimizeReductionPath(searchSymbol, k, Chart);
            var transitiveState = FindTransitiveState(earleme, searchSymbol);
            if (transitiveState != null)
            {
                var topmostItem = new State(transitiveState.Production, transitiveState.Position, transitiveState.Origin);
                if (Chart.Enqueue(k, topmostItem))
                    Log("Complete", k, topmostItem);
            }
            else
            {
                int j = completed.Origin;
                for (int s = 0; s < Chart[j].Count; s++)
                {
                    var state = Chart[j][s];
                    if (IsSourceState(completed.Production.LeftHandSide, state))
                    {
                        int i = state.Origin;
                        var nextState = new State(state.Production, state.Position + 1, i);
                        if (Chart.Enqueue(k, nextState))
                            Log("Complete", k, nextState);
                    }
                }
            }
        }

        private void OptimizeReductionPath(ISymbol searchSymbol, int k, Chart chart)
        {
            IState t_rule = null;
            OptimizeReductionPathRecursive(searchSymbol, k, chart, ref t_rule);
        }

        private void OptimizeReductionPathRecursive(ISymbol searchSymbol, int k, Chart chart, ref IState t_rule)
        {
            var list = chart[k];
            var earleme = chart.Earlemes[k];
            var transitiveState = FindTransitiveState(earleme, searchSymbol);
            if (transitiveState != null)
            {
                t_rule = transitiveState;
            }
            else
            {
                var sourceState = FindSourceState(list, searchSymbol);

                if (sourceState != null)
                {
                    var sourceStateNext = sourceState.NextState();
                    if (IsQuasiComplete(sourceStateNext))
                    {
                        t_rule = sourceStateNext;
                        OptimizeReductionPathRecursive(sourceState.Production.LeftHandSide, sourceState.Origin, chart, ref t_rule);
                        if (t_rule != null)
                        {
                            var transitionItem = new TransitionState(
                                searchSymbol,
                                t_rule.Production,
                                t_rule.Production.RightHandSide.Count,
                                t_rule.Origin);
                            if (chart.Enqueue(k, transitionItem))
                                Log("Transition", k, transitionItem);
                        }
                    }
                }
            }
        }

        bool IsQuasiComplete(IState state)
        {            
            return state.IsComplete();
        }

        IState FindSourceState(IReadOnlyList<IState> list, ISymbol searchSymbol)
        {
            var sourceItemCount = 0;
            IState sourceItem = null;
            for (int s = 0; s < list.Count; s++)
            {
                var state = list[s];
                if (IsSourceState(searchSymbol, state))
                {
                    bool moreThanOneSourceItemExists = sourceItemCount > 0;
                    if (moreThanOneSourceItemExists)
                        return null;
                    sourceItemCount++;
                    sourceItem = state;
                }
            }
            return sourceItem;
        }

        private bool IsSourceState(ISymbol searchSymbol, IState state)
        {
            if (state.IsComplete())
                return false;
            return state.CurrentSymbol().Equals(searchSymbol);
        }

        private IState FindTransitiveState(IEarleme earleme, ISymbol searchSymbol)
        {
            for (int t = 0; t < earleme.Transitions.Count; t++)
            {
                var transitionState = earleme.Transitions[t] as TransitionState;
                if (transitionState.Recognized.Equals(searchSymbol))
                    return transitionState;
            }
            return null;
        }
        
        public void Reset()
        {
            Initialization();
        }

        public bool IsAccepted()
        {
            var lastColumn = Chart[Chart.Count - 1];
            return lastColumn
                .Any(x => x.IsComplete() 
                    && x.Origin == 0 
                    && x.Production.LeftHandSide.Value == Grammar.Start.Value);
        }

        private void Log(string operation, int origin, IState state)
        {
            Debug.Write(string.Format("{0}\t{1}", origin, state));
            Debug.WriteLine(string.Format("\t # {0}", operation));
        }

        private void LogScan(int origin, IState state, char token)
        {
            Debug.Write(string.Format("{0}\t{1}", origin, state));
            Debug.WriteLine(string.Format("\t # Scan {0}", token));
        }
    }
}
