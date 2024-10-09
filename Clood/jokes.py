import random

class Jokes:
    def __init__(self):
        self.jokes = [
            "Why don't scientists trust atoms? Because they make up everything!",
            "Why did the scarecrow win an award? He was outstanding in his field!",
            "Why don't eggs tell jokes? They'd crack each other up!",
            "Why did the math book look so sad? Because it had too many problems.",
            "What do you call a fake noodle? An impasta!"
        ]
    
    def get_joke(self):
        return random.choice(self.jokes)
