using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    private static Dictionary<int, Dictionary<int, float>> _ratings;

    static void Main()
    {
        string[] lines = File.ReadAllLines(@"C:\Users\Leon\Documents\Data sets\Group lens dev\ratings.csv");
        _ratings = new Dictionary<int, Dictionary<int, float>>();

        foreach (string line in lines.Skip(1))
        {
            string[] split = line.Split(',');
            int userId = int.Parse(split[0]);
            int movieId = int.Parse(split[1]);
            float rating = float.Parse(split[2]);

            if (!_ratings.ContainsKey(userId))
            {
                _ratings.Add(userId, new Dictionary<int, float>());
            }
            _ratings[userId].Add(movieId, rating);
        }

        #region debug distance algorithms
        //Console.WriteLine("User 10:");
        //foreach (KeyValuePair<int, float> kvp in _ratings[10])
        //{
        //    Console.WriteLine(string.Format("{0}\t{1}", kvp.Key, kvp.Value));
        //}
        //Console.WriteLine();

        //Console.WriteLine("User 11:");
        //foreach (KeyValuePair<int, float> kvp in _ratings[11])
        //{
        //    Console.WriteLine(string.Format("{0}\t{1}", kvp.Key, kvp.Value));
        //}
        //Console.WriteLine();

        //foreach (int intersectingMovie in GetIntersectingMovies(_ratings[10], _ratings[11]))
        //{
        //    Console.WriteLine(string.Format("{0}\t{1}", _ratings[10][intersectingMovie], _ratings[11][intersectingMovie]));
        //}
        //Console.WriteLine();

        //Console.WriteLine("Euclidian: " + EuclidianDistance(_ratings[10], _ratings[11]));
        //Console.WriteLine("Pearson: " + PearsonCorrelation(_ratings[10], _ratings[11]));

        //for (int i = 1; i < _ratings.Count - 2; i++)
        //{
        //    Console.WriteLine(string.Format("{0} - {1}", EuclidianDistance(_ratings[i], _ratings[i + 1]), PearsonCorrelation(_ratings[i], _ratings[i + 1])));
        //}
        #endregion

        #region debug top matches

        //IEnumerable<float> topMatches = GetTopMatches(10, EuclidianDistance, 10);

        #endregion

        #region debug recommendations

        IOrderedEnumerable<KeyValuePair<float, int>> recommendations = GetRecommendations(10, EuclidianDistance);

        #endregion

        Console.ReadKey();
    }

    public static float EuclidianDistance(Dictionary<int, float> user1, Dictionary<int, float> user2)
    {
        int[] intersectingMovies = GetIntersectingMovies(user1, user2).ToArray();

        if (intersectingMovies.Length == 0) return 0;

        double sumOfSquares = intersectingMovies.Sum(x => Math.Pow(user1[x] - user2[x], 2));

        return (float) (1 / (1 + Math.Sqrt(sumOfSquares)));
    }

    static float PearsonCorrelation(Dictionary<int, float> user1, Dictionary<int, float> user2)
    {
        int[] intersectingMovies = GetIntersectingMovies(user1, user2).ToArray();
        int intersectingMoviesLength = intersectingMovies.Length;

        if (intersectingMoviesLength == 0) return 0;

        float user1Sum = 0;
        float user2Sum = 0;
        float user1SumOfSquares = 0;
        float user2SumOfSquares = 0;
        float productSum = 0;

        foreach (int intersectingMovie in intersectingMovies)
        {
            user1Sum += user1[intersectingMovie];
            user2Sum += user2[intersectingMovie];
            user1SumOfSquares += (float) Math.Pow(user1[intersectingMovie], 2);
            user2SumOfSquares += (float) Math.Pow(user2[intersectingMovie], 2);

            productSum += user1[intersectingMovie] * user2[intersectingMovie];
        }

        float num = productSum - user1Sum * user2Sum / intersectingMoviesLength;
        float den = (float)
            Math.Sqrt((user1SumOfSquares - Math.Pow(user1Sum, 2) / intersectingMoviesLength) *
                      (user2SumOfSquares - Math.Pow(user2Sum, 2) / intersectingMoviesLength));

        if (Math.Abs(den) < 0.00001) return 0;

        return num / den;
    }

    static IEnumerable<float> GetTopMatches(int userId, Func<Dictionary<int, float>, Dictionary<int, float>, float> algorithm, int amount)
    {
        Dictionary<int, float> person = _ratings[userId];

        IEnumerable<float> scores = _ratings
            .Where(x => x.Key != userId)
            .Select(x => algorithm(person, x.Value))
            .OrderByDescending(x => x)
            .Take(amount);

        return scores;
    }

    static IOrderedEnumerable<KeyValuePair<float, int>> GetRecommendations(int userId, Func<Dictionary<int, float>, Dictionary<int, float>, float> algorithm)
    {
        Dictionary<int, float> person = _ratings[userId];
        var totals = new Dictionary<int, float>();
        var similaritySums = new Dictionary<int, float>();

        foreach (KeyValuePair<int, Dictionary<int, float>> user in _ratings.Where(x => x.Key != userId))
        {
            float similarity = algorithm(person, user.Value);

            if (Math.Abs(similarity) <= 0.00001) continue;

            foreach (KeyValuePair<int, float> movie in user.Value)
            {
                if (!person.ContainsKey(movie.Key) || Math.Abs(person[movie.Key]) < 0.00001)
                {
                    if (!totals.ContainsKey(movie.Key)) totals.Add(movie.Key, 0);
                    if (!similaritySums.ContainsKey(movie.Key)) similaritySums.Add(movie.Key, 0);

                    totals[movie.Key] += movie.Value * similarity;
                    similaritySums[movie.Key] += similarity;
                }
            }
        }

        IEnumerable<KeyValuePair<float, int>> rankings = totals.Select(x => new KeyValuePair<float, int>(x.Value / similaritySums[x.Key], x.Key));

        IOrderedEnumerable<KeyValuePair<float, int>> orderedRankings = rankings.OrderByDescending(x => x.Key);

        return orderedRankings;
    }
    
    private static IEnumerable<int> GetIntersectingMovies(Dictionary<int, float> user1, Dictionary<int, float> user2)
    {
        var intersectingMovies = new List<int>();

        foreach (KeyValuePair<int, float> rating in user1)
        {
            if (user2.ContainsKey(rating.Key))
            {
                intersectingMovies.Add(rating.Key);
            }
        }

        return intersectingMovies;
    }
}