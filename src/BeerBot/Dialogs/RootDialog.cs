using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeerBot.BeerApi.Client.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace BeerBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog
    {
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
                await context.PostAsync("Howdy! How can I help you?");
                context.Wait(MessageReceivedAsync);
            }
            else if (Regex.IsMatch(message.Text, "^(bye|adios).*", RegexOptions.IgnoreCase))
            {
                await context.PostAsync("So soon? Oh well. See you later :)");
                context.Done(true);
            }
            else
            {
                await MessageReceivedAsync(context, argument);
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
                context.Call(RecommendationDialog.Dialog, BeerRecommendedAsync);
                return;
            }
            if (Regex.IsMatch(message.Text, "^help.*", RegexOptions.IgnoreCase))
            {
                await context.PostAsync("You can type 'order' for ordering beers and 'recommend' for getting some recommendations");
                context.Wait(MessageReceivedAsync);
            }
            else if (Regex.IsMatch(message.Text, "^(bye|adios).*", RegexOptions.IgnoreCase))
            {
                await context.PostAsync("Thank you. Come again!");
                context.Done(true);
            }
            else
            {
                await context.PostAsync("I don't quite understand. Please type 'help' for getting acquianted with me.");
                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task BeerOrderedAsync(IDialogContext context, IAwaitable<BeerOrder> result)
        {
            var beerOrder = await result;
            await context.PostAsync($"Your order of {beerOrder.BeerName} and {beerOrder.Chaser} with {beerOrder.Side} is coming right up!");
            await context.PostAsync("So what would you like to do next?");
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
                await context.PostAsync("So what would you like to do next?");
                context.Wait(MessageReceivedAsync);
            }
            _recommendedBeer = null;
        }
    }
}