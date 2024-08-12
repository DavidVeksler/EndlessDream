using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var service = new LlmService();

Console.WriteLine("🚀 Sending request to LLM...");
var prompt = "How do I init and update a git submodule?";

var (wordCount, tokenCount, elapsedMs) = await service.StreamCompletionAsync(prompt, Console.Write);

Console.WriteLine($"\n\n📊 Stats: {wordCount} words, {tokenCount} tokens, {elapsedMs}ms");
Console.WriteLine("✅ Response complete!");