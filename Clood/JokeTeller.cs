using System;
using System.Collections.Generic;

namespace CloodAndFriends.Clood
{
    public class JokeTeller
    {
        private List<string> jokes;

        public JokeTeller()
        {
            jokes = new List<string>
            {
                "Why don't scientists trust atoms? Because they make up everything!",
                "Why did the scarecrow win an award? He was outstanding in his field!",
                "Why don't eggs tell jokes? They'd crack each other up!",
                "What do you call a fake noodle? An impasta!",
                "Why did the math book look so sad? Because it had too many problems!"
            };
        }

        public string TellJoke()
        {
            Random random = new Random();
            int index = random.Next(jokes.Count);
            return jokes[index];
        }

        public void TellAllJokes()
        {
            Console.WriteLine("Get ready for some hilarious jokes!");
            foreach (string joke in jokes)
            {
                Console.WriteLine(joke);
                Console.WriteLine();
            }
        }
    }
}
