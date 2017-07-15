using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Autofac;
using BeerBot.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace BeerBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        [ResponseType(typeof(void))]
        public virtual async Task<IHttpActionResult> Post([FromBody] Activity activity)
        {
            if (activity == null)
                return BadRequest();

            switch (activity.GetActivityType())
            {
                case ActivityTypes.Message:
                    //await Conversation.SendAsync(activity, () => new RootDialog());
                    await Conversation.SendAsync(activity, () => new RootLuisDialog());
                    break;
                case ActivityTypes.ConversationUpdate:
                    await HandleConversationUpdate(activity);
                    break;
                default:
                    Trace.TraceWarning($"Unhandled activity type recieved: '{activity.Type}'");
                    break;
            }
            return StatusCode(HttpStatusCode.Accepted);
        }

        private static async Task HandleConversationUpdate(Activity activity)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var client = scope.Resolve<IConnectorClient>();
                if (activity.MembersAdded.Any())
                {
                    var reply = activity.CreateReply();
                    foreach (var newMember in activity.MembersAdded)
                    {
                        if (MemberIsBot(activity, newMember)) continue;

                        reply.Text = $"Hi there {newMember.Name}! Welcome to your friendly neighborhood bot-tender :)";
                        await client.Conversations.ReplyToActivityAsync(reply);
                    }
                }
            }
        }

        private static bool MemberIsBot(IActivity activity, ChannelAccount newMember)
        {
            return newMember.Id == activity.Recipient.Id;
        }
    }
}