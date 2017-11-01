using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPA.ContinuousAlgorithms
{
    public class SimulatedAnnealing : StochasticHillClimber
    {
        protected double mT0 = 100;
        protected int mMCSteps = 100;
        protected double mCoolingRate = 0.99;

        public SimulatedAnnealing(double[] masks, double T0, int monte_carlo_steps, double cooling_rate)
            : base(masks)
        {
            mT0 = T0;
            mMCSteps = monte_carlo_steps;
            mCoolingRate = cooling_rate;
        }

        public double T0
        {
            get { return mT0; }
            set { mT0 = value; }
        }

        public int MonteCarloSteps
        {
            get { return mMCSteps; }
            set { mMCSteps = value; }
        }

        public double CoolingRate
        {
            get { return mCoolingRate; }
            set { mCoolingRate = value; }
        }

        protected virtual void DecreaseTemperature(ref double T)
        {
            T = T * mCoolingRate;
        }

        public override ContinuousSolution Minimize(double[] x_0, CostEvaluationMethod evaluate, GradientEvaluationMethod calc_gradient, TerminationEvaluationMethod should_terminate, object constraints = null)
        {
            double? improvement = null;
            int iteration = 0;

            double fx_0 = evaluate(x_0, mLowerBounds, mUpperBounds, constraints);
            ContinuousSolution best_solution = new ContinuousSolution(x_0, fx_0);

            double T = mT0;
            while (!should_terminate(improvement, iteration))
            {
                for (int i = 0; i < mMCSteps; ++i)
                {
                    double[] x_pi = GetNeighbor(best_solution.Values, i, constraints);
                    double fx_pi = evaluate(x_pi, mLowerBounds, mUpperBounds, constraints);

                    if (best_solution.TryUpdateSolution(x_pi, fx_pi, out improvement))
                    {
                        OnSolutionUpdated(best_solution, iteration);
                    }
                    else
                    {
                        double P = System.Math.Exp((best_solution.Cost - fx_pi) / T);
                        double r = RandomEngine.NextDouble();

                        if (r <= P)
                        {
                            best_solution = new ContinuousSolution(x_pi, fx_pi);
                            OnSolutionUpdated(best_solution, iteration);
                        }
                    }
                }

                DecreaseTemperature(ref T);

                OnStepped(best_solution, iteration);
                iteration++;
            }

            return best_solution;
        }
    }
}
