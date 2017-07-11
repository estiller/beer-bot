using System;
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
                await context.PostAsync("Ordering will be available soon!");
            }
            else if (Regex.IsMatch(message.Text, ".*recommend.*", RegexOptions.IgnoreCase))
            {
                context.Call(RecommendationDialog.Dialog, BeerRecommendedAsync);
                return;
            }
            else if (Regex.IsMatch(message.Text, "^help.*", RegexOptions.IgnoreCase))
            {
                await context.PostAsync("You can type 'order' for ordering beers and 'recommend' for getting some recommendations");
            }
            else if (Regex.IsMatch(message.Text, "^(bye|adios).*", RegexOptions.IgnoreCase))
            {
                await context.PostAsync("Thank you. Come again!");
                context.Done(true);
                return;
            }
            else
            {
                await context.PostAsync("I don't quite understand. Please type 'help' for getting acquianted with me.");
            }
            context.Wait(MessageReceivedAsync);
        }

        private async Task BeerRecommendedAsync(IDialogContext context, IAwaitable<Beer> argument)
        {
            var beer = await argument;
            PromptDialog.Confirm(context, async (ctx, res) =>
            {
                bool shouldOrder = await res;
                if (shouldOrder)
                {
                    await ctx.PostAsync("Ordering will be available soon!");
                }
                await ctx.PostAsync("So what would you like to do next?");
                ctx.Wait(MessageReceivedAsync);
            }, $"Would you like to order '{beer.Name}'?");
        }
    }
}