using System;
using System.Configuration;
using System.Linq;
using BeerBot.BeerApi.Client;
using BeerBot.BeerApi.Client.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;

namespace BeerBot.Dialogs
{
    [Serializable]
    public class BeerOrder
    {
        [Prompt("What beer would you like?")]
        public string BeerName { get; set; }
        [Prompt("Which chaser would you like next to your beer? {||}")]
        public Chaser Chaser { get; set; }
        [Prompt("How about something to eat? {||}")]
        public SideDish Side { get; set; }
    }

    public enum Chaser
    {
        [Describe("Whiskey")]
        Whiskey = 1,
        [Describe("Vodka")]
        Vodka,
        [Describe("Liquor")]
        Liquor,
        [Describe("Water")]
        Water
    }

    public enum SideDish
    {
        Fries = 1,
        Pretzels,
        Nachos
    }

    public static class OrderDialog
    {
        private static readonly IBeerAPI BeerApiClient = new BeerAPI(new Uri(ConfigurationManager.AppSettings["BeerApiUrl"]));

        public static IDialog<BeerOrder> CreateDialog(string beerName = null)
        {
            return new FormDialog<BeerOrder>(new BeerOrder {BeerName = beerName }, BuildForm, FormOptions.PromptInStart);
        }

        private static IForm<BeerOrder> BuildForm()
        {
            return new FormBuilder<BeerOrder>()
                .Field(nameof(BeerOrder.BeerName), validate: async (state, value) =>
                {
                    var beerName = (string) value;
                    var possibleBeers = await BeerApiClient.BeersGetBySearchTermAsync(beerName);
                    
                    Beer exactMatch = possibleBeers.FirstOrDefault(b => b.Name.Equals(beerName, StringComparison.CurrentCultureIgnoreCase));
                    if (exactMatch != null)
                    {
                        return new ValidateResult { IsValid = true, Value = possibleBeers[0].Name };
                    }

                    switch (possibleBeers.Count)
                    {
                        case 0:
                            return new ValidateResult { IsValid = false, Feedback = "Don't know such beer... Try again." };
                        case 1:
                            return new ValidateResult {IsValid = true, Value = possibleBeers[0].Name};
                    }
                    return new ValidateResult
                    {
                        IsValid = false,
                        Feedback = "I'm not sure which one", 
                        Choices = possibleBeers.Select(b => new Choice
                        {
                            Value = b.Name,
                            Description = new DescribeAttribute(b.Name),
                            Terms = new TermsAttribute(b.Name)
                        })
                    };
                })
                .AddRemainingFields()
                .Build();
        }
    }
}