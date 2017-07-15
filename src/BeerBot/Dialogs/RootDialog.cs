using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client.Models;
using BeerBot.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace BeerBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog
    {
        private const string LastBeerOrderedKeyName = "LastBeerOrdered";
        private Beer _recommendedBeer;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(InitialMessageReceivedAsync);
            return Task.FromResult((object) null);
        }

        private async Task InitialMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            if (Regex.IsMatch(message.Text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase))
            {
                if (context.UserData.TryGetValue(LastBeerOrderedKeyName, out string beerName))
                {
                    PromptDialog.Confirm(context, ConfirmOrderLastKnownBeerAsync, $"Would you like to order your usual {beerName}?");
                }
                else
                {
                    await context.SpeakAsync("Howdy! How can I help you?", InputHints.ExpectingInput);
                    context.Wait(MessageReceivedAsync);
                }
            }
            else if (Regex.IsMatch(message.Text, "^(bye|adios).*", RegexOptions.IgnoreCase))
            {
                await context.SpeakAsync("So soon? Oh well. See you later :)", InputHints.ExpectingInput);
                context.Done(true);
            }
            else
            {
                await MessageReceivedAsync(context, argument);
            }
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
                await context.SpeakAsync("No problem. So how can I help you?", InputHints.ExpectingInput);
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            if (Regex.IsMatch(message.Text, ".*order.*", RegexOptions.IgnoreCase))
            {
                context.Call(OrderDialog.CreateDialog(), BeerOrderedAsync);
                return;
            }
            if (Regex.IsMatch(message.Text, ".*recommend.*", RegexOptions.IgnoreCase))
            {
                context.Call(RecommendationDialog.CreateDialog(null, null, null, null), BeerRecommendedAsync);
                return;
            }
            if (Regex.IsMatch(message.Text, "^help.*", RegexOptions.IgnoreCase))
            {
                await context.SpeakAsync("You can type 'order' for ordering beers and 'recommend' for getting some recommendations", InputHints.ExpectingInput);
                context.Wait(MessageReceivedAsync);
            }
            else if (Regex.IsMatch(message.Text, "^(bye|adios).*", RegexOptions.IgnoreCase))
            {
                await context.SpeakAsync("Thank you. Come again!", InputHints.ExpectingInput);
                context.Done(true);
            }
            else
            {
                await context.SpeakAsync("I don't quite understand. Say 'help' for getting acquianted with me.", InputHints.ExpectingInput);
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task BeerOrderedAsync(IDialogContext context, IAwaitable<BeerOrder> result)
        {
            var beerOrder = await result;
            await context.SpeakAsync($"Your order of {beerOrder.BeerName} and {beerOrder.Chaser} with {beerOrder.Side} is coming right up!");
            context.UserData.SetValue(LastBeerOrderedKeyName, beerOrder.BeerName);
            await context.SpeakAsync("So what would you like to do next?", InputHints.ExpectingInput);
            context.Wait(MessageReceivedAsync);
        }

        private async Task BeerRecommendedAsync(IDialogContext context, IAwaitable<Beer> argument)
        {
            Debug.Assert(_recommendedBeer == null);
            _recommendedBeer = await argument;

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
                await context.SpeakAsync("So what would you like to do next?", InputHints.ExpectingInput);
                context.Wait(MessageReceivedAsync);
            }
            _recommendedBeer = null;
        }
    }
}