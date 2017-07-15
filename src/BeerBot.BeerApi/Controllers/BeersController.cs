using System.Collections.Generic;
using System.Linq;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Extensions;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace BeerBot.BeerApi.Controllers
{
    [Route("api/beers")]
    public class BeersController : Controller
    {
        private readonly IRepository<Beer> _beerRepository;
        private readonly IRepository<Brewery> _breweryRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Style> _styleRepository;

        public BeersController(IRepository<Beer> beerRepository, IRepository<Brewery> breweryRepository, IRepository<Category> categoryRepository, IRepository<Style> styleRepository)
        {
            _beerRepository = beerRepository;
            _breweryRepository = breweryRepository;
            _categoryRepository = categoryRepository;
            _styleRepository = styleRepository;
        }

        [HttpGet]
        public IEnumerable<Beer> Get(
            [FromQuery(Name = "searchTerm")] string[] searchTerms,
            [FromQuery(Name = "breweryId")] int[] breweryIds,
            [FromQuery(Name = "breweryName")] string[] breweryNames,
            [FromQuery(Name = "country")] string[] countries,
            [FromQuery(Name = "categoryId")] int[] categoryIds,
            [FromQuery(Name = "categoryName")] string[] categoryNames,
            [FromQuery(Name = "styleId")] int[] styleIds,
            [FromQuery(Name = "styleName")] string[] styleNames,
            float? minAbv, float? maxAbv)
        {
            var additionalBreweryIds = _breweryRepository.Get()
                .EmptyIfNoFilters(breweryNames, countries)
                .FilterBySearchTerms(breweryNames, b => b.Name)
                .FilterByCaseInsensitive(countries, b => b.Country)
                .Select(b => b.Id);

            var additionalCategoryIds = _categoryRepository.Get()
                .EmptyIfNoFilters(categoryNames)
                .FilterByCaseInsensitive(categoryNames, c => c.Name)
                .Select(c => c.Id);

            var additionalStyleIds = _styleRepository.Get()
                .EmptyIfNoFilters(styleNames)
                .FilterByCaseInsensitive(styleNames, s => s.Name)
                .Select(s => s.Id);

            return _beerRepository.Get()
                .FilterBySearchTerms(searchTerms, b => b.Name)
                .FilterBy(breweryIds.Concat(additionalBreweryIds).ToList(), b => b.BreweryId)
                .FilterBy(categoryIds.Concat(additionalCategoryIds).ToList(), b => b.CategoryId)
                .FilterBy(styleIds.Concat(additionalStyleIds).ToList(), b => b.StyleId)
                .FilterByRange(minAbv, maxAbv, b => b.Abv);
        }

        [HttpGet("{id}")]
        public Beer Get(int id)
        {
            return _beerRepository.GetById(id);
        }
    }
}
