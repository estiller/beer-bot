using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace BeerBot.Extensions
{
    public static class ContextExtensions
    {
        public static Task SpeakAsync(this IBotToUser context, string text, string inputHint = null, string speak = null)
        {
            return context.SayAsync(text, speak ?? text, inputHint != null ? new MessageOptions {InputHint = inputHint} : null);
        }
    }
}