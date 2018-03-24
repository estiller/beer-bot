using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client;
using BeerBot.BeerApi.Client.Models;
using BeerBot.Extensions;
using Microsoft.Bot.Builder.Dialogs;

namespace BeerBot.Dialogs
{
    public static class RecommendationDialog
    {
        private static readonly IBeerAPI BeerApiClient = new BeerAPI(new Uri(ConfigurationManager.AppSettings["BeerApiUrl"]));

        public static IDialog<Beer> CreateDialog(string beerName, string brewery, string category, string country)
        {
            IDialog<Beer> dialog;
            if (beerName == null && brewery == null && category == null && country == null)
            {
                dialog = Chain
                    .From(() => new PromptDialog.PromptChoice<RecommendationOptions>(new PromptOptions<RecommendationOptions>(
                        "How would you like me to recommend your beer?", 
                        "Not sure I got it. Could you try again?", 
                        options: new[] { RecommendationOptions.Category, RecommendationOptions.Origin, RecommendationOptions.Name },
                        descriptions: new[] { "By Beer Category", "By Beer Origin", "By Beer Name" }, 
                        speak: "How would you like me to recommend your beer? By category, by origin, or by name?", 
                        retrySpeak: "Not sure I got it. Could you try again?")))
                    .Switch(
                        Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Category, (context, option) => CategoryRecommendation),
                        Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Origin, (context, option) => CountryRecommendation),
                        Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Name, (context, option) => NameRecommendation)
                    )
                    .Unwrap();
            }
            else
            {
                dialog = Chain.Return(new RecommendationFilter { BeerName = beerName, Brewery = brewery, Category = category, Country = country})
                    .ContinueWith(
                    (context, awaitable) => ChooseBeer(context, awaitable, async filter => await BeerApiClient.BeersGetAsync(filter.BeerName, filter.Brewery, filter.Category, filter.Country), null));
            }

            return dialog.ContinueWith(async (context, beerAwaitable) => Chain.Return(await beerAwaitable));
        }

        private enum RecommendationOptions
        {
            Category = 1,
            Origin,
            Name
        }

        [Serializable]
        private class RecommendationFilter
        {
            public string BeerName { get; set; }
            public string Brewery { get; set; }
            public string Category { get; set; }
            public string Country { get; set; }
        }

        private static readonly IDialog<Beer> CategoryRecommendation = Chain
            .From(() =>
            {
                var categories = BeerApiClient.CategoriesGet().ToArray();
                return new PromptDialog.PromptChoice<Category>(new PromptOptions<Category>(
                    "Which kind of beer do you like?", 
                    "I probably drank too much. Which beer type was it?", 
                    options: categories, 
                    speak: "Which kind of beer do you like?", 
                    retrySpeak: "I probably drank too much. Which beer type was it?"));
            })
            .ContinueWith<Category, Style>(async (context, categoryAwaitable) =>
            {
                var category = await categoryAwaitable;
                var styles = (await BeerApiClient.StylesGetByCategoryAsync(category.Id)).ToArray();
                return new PromptDialog.PromptChoice<Style>(new PromptOptions<Style>(
                    "Which style?",
                    "I probably drank too much. Which style was it?",
                    options: styles,
                    speak: "Which style?",
                    retrySpeak: "I probably drank too much. Which style was it?"));
            })
            .ContinueWith((context, styleAwaitable) => ChooseBeer(context, styleAwaitable, async style => await BeerApiClient.BeersGetByStyleAsync(style.Id), CategoryRecommendation));

        private static readonly IDialog<Beer> CountryRecommendation = Chain
            .From(() =>
            {
                var countries = BeerApiClient.BreweriesCountriesGet().Random(5).ToArray();
                return new PromptDialog.PromptChoice<string>(new PromptOptions<string>(
                    "Where would you like your beer from?",
                    "I probably drank too much. Where would you like your beer from?",
                    options: countries,
                    speak: "Where would you like your beer from?",
                    retrySpeak: "I probably drank too much. Where would you like your beer from?"));
            })
            .ContinueWith(ChooseBrewery)
            .ContinueWith((context, breweryAwaitable) => ChooseBeer(context, breweryAwaitable, async brewery => await BeerApiClient.BeersGetByBreweryAsync(brewery.Id), CountryRecommendation));

        private static readonly IDialog<Beer> NameRecommendation = Chain
            .From(() => new PromptDialog.PromptString(new PromptOptions<string>(
                "Do you remember the name? Give me what you remember", "I probably drank too much. What was the name?", 
                speak: "Do you remember the name? Give me what you remember", retrySpeak: "I probably drank too much. What was the name?")))
            .ContinueWith((context, termAwaitable) => ChooseBeer(context, termAwaitable, async searchTerm => await BeerApiClient.BeersGetBySearchTermAsync(searchTerm), NameRecommendation));

        private static async Task<IDialog<Brewery>> ChooseBrewery(IBotContext context, IAwaitable<string> countryAwaitable)
        {
            var country = await countryAwaitable;
            var breweries = (await BeerApiClient.BreweriesGetByCountryAsync(country)).Random(5).ToList();
            Debug.Assert(breweries.Count > 0, "There is no country in the list with zero breweries!");
            if (breweries.Count == 1)
            {
                await context.SpeakAsync($"Then you need a beer made by {breweries[0].Name}");
                return Chain.Return(breweries[0]);
            }
            return new PromptDialog.PromptChoice<Brewery>(new PromptOptions<Brewery>(
                "Which brewery?", "I probably drank too much. Which brewery was it?", options: breweries, speak: "Which brewery?", retrySpeak: "I probably drank too much. Which brewery was it?"));
        }

        private static async Task<IDialog<Beer>> ChooseBeer<T>(IBotContext context, IAwaitable<T> awaitableArgument, Func<T, Task<IEnumerable<Beer>>> beerSelector, IDialog<Beer> retryDialog)
        {
            T argument = await awaitableArgument;
            var recommendation = (await beerSelector(argument)).Random(3).ToList();
            switch (recommendation.Count)
            {
                case 0:
                    await context.SpeakAsync("Oops! I havn't found any beer!");
                    return retryDialog ?? Chain.Return<Beer>(null);
                case 1:
                    await context.SpeakAsync("Eureka! I've got a beer for you");
                    return Chain.Return(recommendation[0]);
                default:
                    return new PromptDialog.PromptChoice<Beer>(new PromptOptions<Beer>(
                        "Which one of these works?", "I probably drank too much. Which one of these work?", options: recommendation,
                        speak: "Which one of these works?", retrySpeak: "I probably drank too much. Which one of these work?"));
            }
        }
    }
}