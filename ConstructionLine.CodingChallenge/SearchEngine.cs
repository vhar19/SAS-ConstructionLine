using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConstructionLine.CodingChallenge
{
    public class SearchEngine
    {
        private readonly Dictionary<Guid, Shirt[]> _shirtsPerSize; 
        private readonly Dictionary<Guid, Shirt[]> _shirtsPerColor;

        public SearchEngine(List<Shirt> shirts)
        {
            _shirtsPerColor = shirts.GroupBy(x => x.Color.Id).ToDictionary(x => x.Key, x=>x.ToArray());
            _shirtsPerSize = shirts.GroupBy(x => x.Size.Id).ToDictionary(x => x.Key, x=> x.ToArray());
        }


        public SearchResults Search(SearchOptions options)
        {
            var searchByColor = SearchByColorTask(options.Colors.Select(x => x.Id).ToArray());

            var searchBySize = SearchBySizeTask(options.Sizes.Select(x => x.Id).ToArray());
            
            RunInParallel(searchByColor, searchBySize);

            var intersectionResults = searchByColor.Result.Intersect(searchBySize.Result).ToArray();

            var colorSummary = BuildColorSummary(intersectionResults);

            var sizesSummary = BuildSizesSummary(intersectionResults);

            RunInParallel(colorSummary, sizesSummary);

            return new SearchResults
            {
                Shirts = intersectionResults.ToList(),
                ColorCounts = colorSummary.Result,
                SizeCounts = sizesSummary.Result
            };
        }



        private void RunInParallel(params Task[] tasks)
        {
            Task.WhenAll(tasks);
            var failedTask = tasks.FirstOrDefault(x => x.IsFaulted);
            if (failedTask != null)
            {
                throw new Exception("One or more search tasks failed", failedTask.Exception);
            }
        }

        private Task<Shirt[]> SearchByColorTask(Guid[] colors)
        {
            var colorFilter = colors.Any() ? colors : Color.All.Select(x => x.Id).ToArray();  
            var results = new List<Shirt>();
            foreach (var color in colorFilter)
            {
                if (_shirtsPerColor.TryGetValue(color, out var shirts))
                {
                    results.AddRange(shirts);
                }
            }
            return Task.FromResult(results.ToArray());
        }

        private Task<Shirt[]> SearchBySizeTask(Guid[] sizes)
        {
            var sizeFilter = sizes.Any() ? sizes : Size.All.Select(x => x.Id).ToArray();
            var results = new List<Shirt>();
            foreach (var size in sizeFilter)
            {
                if (_shirtsPerSize.TryGetValue(size, out var shirts))
                {
                    results.AddRange(shirts);
                }
            }
            return Task.FromResult(results.ToArray());
        }

        private Task<List<ColorCount>> BuildColorSummary(Shirt[] results)
        {
            var colorsSummary = results
                .GroupBy(x => x.Color)
                .Select(c => new ColorCount()
                {
                    Color = c.Key,
                    Count = c.Count()
                }).ToList();
            foreach (var color in Color.All.Except(colorsSummary.Select(x => x.Color)))
            {
                colorsSummary.Add(new ColorCount() { Color = color, Count = 0 });
            }
            return Task.FromResult(colorsSummary);
        }

        private Task<List<SizeCount>> BuildSizesSummary(Shirt[] results)
        {
            var sizesSummary = results
                .GroupBy(x => x.Size)
                .Select(c => new SizeCount()
                {
                    Size = c.Key,
                    Count = c.Count()
                }).ToList();
            foreach (var size in Size.All.Except(sizesSummary.Select(x => x.Size)))
            {
                sizesSummary.Add(new SizeCount() { Size = size, Count = 0 });
            }
            return Task.FromResult(sizesSummary);
        }
       


    }
}