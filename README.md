# ai-npc
**Experiments in building LLM-based NPCs.**

Exactly how large of a language model do you need to run an NPC?

Let's say that we want to populate a generic JRPG town.  There are an expected cast of characters:

* Some number of generate townsfolk that sometimes share game-related story, but often just prattle on about something inconsequential.
* Town guards that often repeat a single stock statement ad-infinitum.
* Shopkeepers.  Items, weapons, armor, etc.
* Some sort of town leader.

The goal of this experiment is to analyze the feasibility of running a local LLM on a mid-grade gaming laptop.

## Small Language Models

I'm going to defer to more of an expert on this matter:
[Small Language Models (SLM): A Comprehensive Overview](https://huggingface.co/blog/jjokah/small-language-model)

	`Small Language Models (SLMs) are lightweight versions of traditional language models designed to operate efficiently on resource-constrained environments such as smartphones, embedded systems, or low-power computers.`

Can we use an SLM to make NPCs just a little bit smarter?

There's a [Llama model](https://huggingface.co/meta-llama/Llama-3.2-1B) with only 1 billion parameters; significantly less than any LLM you'd see today.  The compute power needed to run an model seems to go up quickly as you increase the parameter count, so reducing that as far as is reasonable would be nice.  The quantized edition of this model uses lower-precision math, which might boost the performance even more.  Is it worth it?

The largest caveat here is the licensing.  Llama isn't really open source, and the licensing agreement is full of rules.  Missing a rule can get me in trouble.

[Qwen2.5-0.5B](https://huggingface.co/Qwen/Qwen2.5-0.5B) has half the parameters of the Llama model (and twice the performance??), and it is released under the apache-2.0 license, which I am personally a lot more comfortable with.  Care should still be taken before creating anything commercial with this though.

[Play with Qwen](https://qwen.ai/home)

## Ollama

The ollama server can run the model we need.  A problem here is that, as we are trying to embed this in a game of some kind, we don't want our users to first need to install and configure a server.  How can we streamline this process for them?

[Download Ollama](https://ollama.com/download/linux)

Ollama can be installed from Linux with a single curl command that runs a bash script.

My initial stab at solving this (with lots of AI coding help) will first check to see if a local Ollama is installed.  It will download the Ollama server, install it to a local app storage folder, then run it and download the qwen2.5-0.5b model into itself.  You can review the code in the `features/01-ollama-management` branch to see this at work.

## OllamaSharp

While I *can* write an entire API layer around the OllamaManager, I'd rather not.  It's not the purpose of this experiment.  I'll be testing out the OllamaSharp library:

* [GitHub](https://github.com/awaescher/OllamaSharp)
* [Getting Started](https://deepwiki.com/awaescher/OllamaSharp/2-getting-started)

In the `features/02-ollamasharp` branch I'm bringing in the OllamaSharp library, reimplementing the existing direct calls to Ollama with their OllamaSharp wrappers, and running some chat completions against the Qwen model.  The system is really simple to use.

In another surprise, it looks like the Ollama server is managing the history of my ongoing chat with Qwen.  This is pretty neat, as it means I don't have to figure out how to stuff in the entire conversation context all at once.  On the other hand, I'm going to need to give some thought to how we will implement multiple actors with a single server.

