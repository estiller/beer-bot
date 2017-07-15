using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;

namespace BeerBot.Dialogs
{
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private const string LastBeerOrderedKeyName = "LastBeerOrdered";

        private const string BeerNameEntityName = "beername";
        private const string BreweryEntityName = "brewery";
        private const string CategoryEntityName = "category";
        private const string CountryEntityName = "country";
        private const string ChaserEntityName = "chaser";
        private const string SideDishEntityName = "sidedish";

        private Beer _recommendedBeer;

        public RootLuisDialog() : base(CreateLuisService())
        {
        }

        private static ILuisService CreateLuisService()
        {
            return new LuisService(
                new LuisModelAttribute(ConfigurationManager.AppSettings["LuisModelId"], ConfigurationManager.AppSettings["LuisSubscriptionKey"])
                {
                    SpellCheck = true
                });
        }

        [LuisIntent("None")]
        public async Task OnIntentUnidentifiedAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            await context.PostAsync("I'm sorry, I didn't get that. How can I help you?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetHelp")]
        public async Task OnGetHelpAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            await context.PostAsync("I can recommend a beer for you, or you can go ahead and make an order.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Bye")]
        public async Task OnByeAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            await context.PostAsync("Bye bye. See you soon!");
            context.Done((object) null);
        }

        [LuisIntent("Greet")]
        public async Task OnGreetAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            if (context.UserData.TryGetValue(LastBeerOrderedKeyName, out string beerName))
            {
                PromptDialog.Confirm(context, ConfirmOrderLastKnownBeerAsync, $"Would you like to order your usual {beerName}?");
            }
            else
            {
                await context.PostAsync("Howdy! How can I help you?");
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("RecommendBeer")]
        public Task OnRecommendBeerAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            var beerName = GetEntity(luisResult, BeerNameEntityName);
            var brewery = GetEntity(luisResult, BreweryEntityName);
            var category = GetEntity(luisResult, CategoryEntityName);
            var country = GetEntity(luisResult, CountryEntityName);

            context.Call(RecommendationDialog.CreateDialog(beerName, brewery, category, country), BeerRecommendedAsync);
            return Task.FromResult((object) null);
        }

        [LuisIntent("OrderBeer")]
        public Task OnOrderBeerAsync(IDialogContext context, IAwaitable<IMessageActivity> messageAwaitable, LuisResult luisResult)
        {
            var beerName = GetEntity(luisResult, BeerNameEntityName);
            var chaser = GetEntity(luisResult, ChaserEntityName);
            var sideDish = GetEntity(luisResult, SideDishEntityName);

            context.Call(OrderDialog.CreateDialog(beerName, chaser, sideDish), BeerOrderedAsync);
            return Task.FromResult((object)null);
        }

        private async Task ConfirmOrderLastKnownBeerAsync(IDialogContext context, IAwaitable<bool> isConfirmed)
        {
            if (await isConfirmed)
            {
                var beerName = context.UserData.GetValue<string>(LastBeerOrderedKeyName);
                context.Call(OrderDialog.CreateDialog(beerName), BeerOrderedAsync);
            }
            else
            {
                await context.PostAsync("No problem. So how can I help you?");
                context.Wait(MessageReceived);
            }
        }

        private async Task BeerOrderedAsync(IDialogContext context, IAwaitable<BeerOrder> result)
        {
            var beerOrder = await result;
            await context.PostAsync($"Your order of {beerOrder.BeerName} and {beerOrder.Chaser} with {beerOrder.Side} is coming right up!");
            context.UserData.SetValue(LastBeerOrderedKeyName, beerOrder.BeerName);
            await context.PostAsync("So what would you like to do next?");
            context.Wait(MessageReceived);
        }

        private async Task BeerRecommendedAsync(IDialogContext context, IAwaitable<Beer> argument)
        {
            Debug.Assert(_recommendedBeer == null);
            _recommendedBeer = await argument;
            if (_recommendedBeer == null)
            {
                context.Wait(MessageReceived);
                return;
            }

            // Can't use anonymous method as anonymous method which capture environment artifacts are not serializable
            PromptDialog.Confirm(context, CompleteBeerRecommendationAsync, $"Would you like to order '{_recommendedBeer.Name}'?");
        }

        private async Task CompleteBeerRecommendationAsync(IDialogContext context, IAwaitable<bool> shouldOrder)
        {
            Debug.Assert(_recommendedBeer != null);
            if (await shouldOrder)
            {
                context.Call(OrderDialog.CreateDialog(_recommendedBeer.Name), BeerOrderedAsync);
            }
            else
            {
                await context.PostAsync("So what would you like to do next?");
                context.Wait(MessageReceived);
            }
            _recommendedBeer = null;
        }

        private static string GetEntity(LuisResult luisResult, string entityName)
        {
            var entityRecommendation = luisResult.Entities.FirstOrDefault(e => e.Type == entityName);
            object resolvedValue = null;
            return entityRecommendation?.Resolution?.TryGetValue("values", out resolvedValue) == true 
                ? ((JArray) resolvedValue)[0].ToString() 
                : entityRecommendation?.Entity;
        }
    }
}