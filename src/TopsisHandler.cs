using System.Data;

namespace Topsis.src
{
    public class TopsisHandler
    {
        private List<Index> _decisionMatrix;
        private List<Index> _normalizedVectorDecisionMatrix;
        private List<Index> _normalizedDecisionMatrix;
        private List<Index> _entropyVector;
        private List<Index> _evaluationCriteriaWeights;
        private List<Index> _weightedNormalizedDecisionMatrix;
        private List<Index> _perfectSolutions;
        private List<Index> _worstSolutions;
        private List<Index> _intuitiveProximityToIdealSolution;
        private List<Index> _intuitiveProximityToWorstSolution;
        private List<Index> _relativeProximityCoefficient;
        private List<Index> _rating;

        private bool _print = false;

        public TopsisHandler(List<Index> decisionMatrix)
        {
            _decisionMatrix = new List<Index>(decisionMatrix);
        }

        private void PrintMatrix(List<Index> matrix, string title)
        {
            if (_print is false) return;

            Console.WriteLine("\n\n" + title + "\n");

            var rows = matrix.Select(x => x.Option).Distinct();

            var cols = matrix.Select(x => x.Criterion).Distinct();

            Console.Write("\t");
            foreach (var col in cols)
            {
                Console.Write(col + "\t");
            }

            foreach (var row in rows)
            {
                Console.Write("\n" + row + "\t");

                var line = matrix.Where(x => x.Option == row).Select(x => x.Value);

                foreach (var val in line)
                {
                    Console.Write(Math.Round(val, 3) + "\t");
                }
            }

            Console.WriteLine();
        }

        public Index Handle(bool print = false)
        {
            _print = print;

            PrintMatrix(_decisionMatrix, "Decision matrix");

            CalculateNormalizedVectorDecisionMatrix();

            PrintMatrix(_normalizedVectorDecisionMatrix, "Normalized Vector Decision Matrix");

            CalculateNormalizedDecisionMatrix();

            PrintMatrix(_normalizedDecisionMatrix, "Normalized Decision Matrix");

            CalculateEntropyVector();

            PrintMatrix(_entropyVector, "Entropy Vector");

            CalculateEvaluationCriteriaWeights();

            PrintMatrix(_evaluationCriteriaWeights, "Evaluation Criteria Weights");

            CalculateWeightedNormalizedDecisionMatrix();

            PrintMatrix(_weightedNormalizedDecisionMatrix, "Weighted Normalized Decision Matrix");

            CalculatePerfectSolutions();

            PrintMatrix(_perfectSolutions, "Perfect Solutions");

            CalculateWorstSolutions();

            PrintMatrix(_worstSolutions, "Worst Solutions");

            CalculateIntuitiveProximityToIdealSolution();

            CalculateIntuitiveProximityToWorstSolution();

            CalculateRelativeProximityCoefficient();

            FormRating();

            var list = _intuitiveProximityToIdealSolution
                .Concat(_intuitiveProximityToWorstSolution)
                .Concat(_relativeProximityCoefficient)
                .Concat(_rating)
                .ToList();

            PrintMatrix(list, "Relative proximity to the ideal solution and alternative options rating");

            return _relativeProximityCoefficient.MaxBy(x => x.Value);
        }

        private void CalculateNormalizedVectorDecisionMatrix()
        {
            _normalizedVectorDecisionMatrix = _decisionMatrix.Select(r => new Index()
            {
                Option = r.Option,
                Criterion = r.Criterion,
                Value = r.Value / Math.Sqrt(_decisionMatrix.Where(x => x.Criterion == r.Criterion).Select(x => x.Value * x.Value).Sum())
            }).ToList();
        }

        private void CalculateNormalizedDecisionMatrix()
        {
            _normalizedDecisionMatrix = _decisionMatrix.Select(z => new Index()
            {
                Option = z.Option,
                Criterion = z.Criterion,
                Value = z.Value / _decisionMatrix.Where(x => x.Criterion == z.Criterion).Select(x => x.Value).Sum()
            }).ToList();
        }

        private void CalculateEntropyVector()
        {
            _entropyVector = _normalizedDecisionMatrix.Select(z => new Index()
            {
                Option = "e",
                Criterion = z.Criterion,
                Value = -1 / Math.Log10(_normalizedDecisionMatrix.Select(x => x.Option).Distinct().Count()) *
                    _normalizedDecisionMatrix.Where(x => x.Criterion == z.Criterion).Select(x => x.Value * Math.Log(x.Value)).Sum()
            }).Distinct().ToList();
        }

        private void CalculateEvaluationCriteriaWeights()
        {
            var criteriaWeights = _entropyVector.Select(x => new Index()
            {
                Criterion = x.Criterion,
                Option = x.Option,
                Value = 1 - x.Value
            });

            _evaluationCriteriaWeights = criteriaWeights.Select(d => new Index()
            {
                Option = "w",
                Criterion = d.Criterion,
                Value = d.Value / criteriaWeights.Select(x => x.Value).Sum()
            }).ToList();
        }

        private void CalculateWeightedNormalizedDecisionMatrix()
        {
            _weightedNormalizedDecisionMatrix = _normalizedVectorDecisionMatrix.Select(v => new Index()
            {
                Option = v.Option,
                Criterion = v.Criterion,
                Value = v.Value * _evaluationCriteriaWeights.Where(w => w.Criterion == v.Criterion).First().Value
            }).ToList();
        }

        private void CalculatePerfectSolutions()
        {
            var criterions = _weightedNormalizedDecisionMatrix.Select(x => x.Criterion).Distinct();

            _perfectSolutions = criterions.Select(v => new Index()
            {
                Option = "A+",
                Criterion = v,
                Value = _weightedNormalizedDecisionMatrix.Where(x => x.Criterion == v).Select(x => x.Value).Max()
            }).ToList();
        }

        private void CalculateWorstSolutions()
        {
            var criterions = _weightedNormalizedDecisionMatrix.Select(x => x.Criterion).Distinct();

            _worstSolutions = criterions.Select(v => new Index()
            {
                Option = "A+",
                Criterion = v,
                Value = _weightedNormalizedDecisionMatrix.Where(x => x.Criterion == v).Select(x => x.Value).Min()
            }).ToList();
        }

        private void CalculateIntuitiveProximityToIdealSolution()
        {
            var options = _weightedNormalizedDecisionMatrix.Select(x => x.Option).Distinct();

            var squaresOfDifference = _weightedNormalizedDecisionMatrix
                .Select(x => new Index() 
                { 
                    Option = x.Option,
                    Criterion = x.Criterion,
                    Value = Math.Pow(_perfectSolutions.Where(y => y.Criterion == x.Criterion).First().Value - x.Value, 2)
                });

            _intuitiveProximityToIdealSolution = options.Select(x => new Index()
            {
                Option = x,
                Criterion = "S+",
                Value = Math.Sqrt(squaresOfDifference.Where(y => y.Option == x).Select(y => y.Value).Sum())
            }).ToList();
        }

        private void CalculateIntuitiveProximityToWorstSolution()
        {
            var options = _weightedNormalizedDecisionMatrix.Select(x => x.Option).Distinct();

            var squaresOfDifference = _weightedNormalizedDecisionMatrix
                .Select(x => new Index()
                {
                    Option = x.Option,
                    Criterion = x.Criterion,
                    Value = Math.Pow(_worstSolutions.Where(y => y.Criterion == x.Criterion).First().Value - x.Value, 2)
                });

            _intuitiveProximityToWorstSolution = options.Select(x => new Index()
            {
                Option = x,
                Criterion = "S-",
                Value = Math.Sqrt(squaresOfDifference.Where(y => y.Option == x).Select(y => y.Value).Sum())
            }).ToList();
        }

        private void CalculateRelativeProximityCoefficient()
        {
            _relativeProximityCoefficient = _intuitiveProximityToWorstSolution.Select(x => new Index()
            {
                Option = x.Option,
                Criterion = "C",
                Value = x.Value / (x.Value + _intuitiveProximityToIdealSolution.Where(y => y.Option == x.Option).First().Value)
            }).ToList();
        }

        private void FormRating()
        {
            int i = _decisionMatrix.DistinctBy(x => x.Option).Count();
            _rating = _relativeProximityCoefficient.OrderBy(x => x.Value).Select(x => new Index()
            {
                Option = x.Option,
                Criterion = "Rank",
                Value = i--
            }).ToList();
        }
    }
}
