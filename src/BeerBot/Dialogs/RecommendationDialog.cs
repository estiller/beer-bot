using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client;
using BeerBot.BeerApi.Client.Models;
using BeerBot.Extensions;
using BeerBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace BeerBot.Dialogs
{
    public static class RecommendationDialog
    {
        private static readonly IBeerAPI BeerApiClient = new BeerAPI(new Uri(ConfigurationManager.AppSettings["BeerApiUrl"]));
        private static readonly IImageSearchService ImageSearchService = new ImageSearchService();

        public static readonly IDialog<Beer> Dialog = Chain
            .From(() => new PromptDialog.PromptChoice<RecommendationOptions>(
                new[] { RecommendationOptions.Category, RecommendationOptions.Origin, RecommendationOptions.Name }, 
                "How would you like me to recommend your beer?", 
                "Not sure I got it. Could you try again?",
                3, descriptions: new [] { "By Beer Category" , "By Beer Origin", "By Beer Name" }))
            .Switch(
                Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Category, (context, option) => CategoryRecommendation),
                Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Origin, (context, option) => CountryRecommendation),
                Chain.Case<RecommendationOptions, IDialog<Beer>>(option => option == RecommendationOptions.Name, (context, option) => NameRecommendation)
            )
            .Unwrap()
            .ContinueWith(async (context, beerAwaitable) =>
            {
                var chosenBeer = await beerAwaitable;
                Uri imageUrl = await ImageSearchService.SearchImage($"{chosenBeer.Name} beer");
                var card = new HeroCard("Your beer!", chosenBeer.Name, chosenBeer.Description, new List<CardImage> { new CardImage(imageUrl.ToString()) });

                var message = context.MakeMessage();
                message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                message.Attachments = new List<Attachment> {card.ToAttachment()};
                await context.PostAsync(message);
                return Chain.Return(chosenBeer);
            });

        private enum RecommendationOptions
        {
            Category = 1,
            Origin,
            Name
        }

        private static readonly IDialog<Beer> CategoryRecommendation = Chain
            .From(() =>
            {
                var categories = BeerApiClient.CategoriesGet();
                return new PromptDialog.PromptChoice<Category>(categories, "Which kind of beer do you like?", "I probably drank too much. Which beer type was it?", 3);
            })
            .ContinueWith<Category, Style>(async (context, categoryAwaitable) =>
            {
                var category = await categoryAwaitable;
                var styles = await BeerApiClient.StylesGetByCategoryAsync(category.Id);
                return new PromptDialog.PromptChoice<Style>(styles, "Which style?", "I probably drank too much. Which style was it?", 3);
            })
            .ContinueWith((context, styleAwaitable) => ChooseBeer(context, styleAwaitable, async style => await BeerApiClient.BeersGetByStyleAsync(style.Id), CategoryRecommendation));

        private static readonly IDialog<Beer> CountryRecommendation = Chain
            .From(() =>
            {
                var countries = BeerApiClient.BreweriesCountriesGet().Random(5).ToList();
                return new PromptDialog.PromptChoice<string>(countries, "Where would you like your beer from?", "I probably drank too much. Where would you like your beer from?", 3);
            })
            .ContinueWith(ChooseBrewery)
            .ContinueWith((context, breweryAwaitable) => ChooseBeer(context, breweryAwaitable, async brewery => await BeerApiClient.BeersGetByBreweryAsync(brewery.Id), CountryRecommendation));

        private static readonly IDialog<Beer> NameRecommendation = Chain
            .From(() => new PromptDialog.PromptString("Do you remember the name? Give me what you remember", "I probably drank too much. What was the name?", 3))
            .ContinueWith((context, termAwaitable) => ChooseBeer(context, termAwaitable, async searchTerm => await BeerApiClient.BeersGetBySearchTermAsync(searchTerm), NameRecommendation));

        private static async Task<IDialog<Brewery>> ChooseBrewery(IBotContext context, IAwaitable<string> countryAwaitable)
        {
            var country = await countryAwaitable;
            var breweries = (await BeerApiClient.BreweriesGetByCountryAsync(country)).Random(5).ToList();
            Debug.Assert(breweries.Count > 0, "There is no country in the list with zero breweries!");
            if (breweries.Count == 1)
            {
                await context.PostAsync($"Then you need a beer made by {breweries[0].Name}");
                return Chain.Return(breweries[0]);
            }
            return new PromptDialog.PromptChoice<Brewery>(breweries, "Which brewery?", "I probably drank too much. Which brewery was it?", 3);
        }

        private static async Task<IDialog<Beer>> ChooseBeer<T>(IBotContext context, IAwaitable<T> awaitableArgument, Func<T, Task<IEnumerable<Beer>>> beerSelector, IDialog<Beer> retryDialog)
        {
            T argument = await awaitableArgument;
            var recommendation = (await beerSelector(argument)).Random(3).ToList();
            switch (recommendation.Count)
            {
                case 0:
                    await context.PostAsync("Oops! I havn't found any beer!");
                    return retryDialog;
                case 1:
                    await context.PostAsync("Eureka! I've got a beer for you");
                    return Chain.Return(recommendation[0]);
                default:
                    return new PromptDialog.PromptChoice<Beer>(recommendation, "Which one of these works?", "I probably drank too much. Which one of these work?", 3);
            }
        }
    }
}