using System.Collections.Generic;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client.Models;

namespace BeerBot.BeerApi.Client
{
    public static partial class BeerAPIExtensions
    {
        public static Task<IList<Style>> StylesGetByCategoryAsync(this IBeerAPI operations, int? categoryId)
        {
            return operations.StylesGetAsync(categoryId: new[] { categoryId });
        }

        public static Task<IList<Beer>> BeersGetByStyleAsync(this IBeerAPI operations, int? styleId)
        {
            return operations.BeersGetAsync(styleId: new[] { styleId });
        }

        public static Task<IList<Beer>> BeersGetByBreweryAsync(this IBeerAPI operations, int? breweryId)
        {
            return operations.BeersGetAsync(breweryId: new[] { breweryId });
        }

        public static Task<IList<Beer>> BeersGetBySearchTermAsync(this IBeerAPI operations, string searchTerm)
        {
            return operations.BeersGetAsync(searchTerm: new[] { searchTerm });
        }

        public static Task<IList<Beer>> BeersGetAsync(this IBeerAPI operations, string searchTerm = null, string brewery = null, string category = null, string country = null)
        {
            return operations.BeersGetAsync(
                searchTerm == null ? null : new[] {searchTerm}, 
                breweryName: brewery == null ? null : new[] {brewery}, 
                categoryName: category == null ? null : new[] {category},
                country: country == null ? null : new[] { country });
        }

        public static Task<IList<Brewery>> BreweriesGetByCountryAsync(this IBeerAPI operations, string country)
        {
            return operations.BreweriesGetAsync(country: new[]{ country });
        }
    }
}