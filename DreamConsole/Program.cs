using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var llmService = new LlmService(new HttpClient());

//Console.WriteLine("🚀 Sending request to LLM...");
//var prompt = "How do I init and update a git submodule?";

//var (wordCount, tokenCount, elapsedMs) = await service.StreamCompletionAsync(prompt, null, Console.Write);

//var fullResult = await llmService.StreamCompletionAsync(
//    "What is the price of Bitcoin?",
//    //systemPrompt: "You are a quantum physics expert. Explain concepts simply.",
//    onContent: Console.Write,
//    temperature: 0.8f,
//    maxTokens: 200
//);

//var fullResult = await llmService.StreamCompletionAsync(
//    "What is the weather in Denver?",
//    //systemPrompt: "You are a quantum physics expert. Explain concepts simply.",
//  //  onContentAsync: Console.Write,
//    temperature: 0.8f,
//    maxTokens: 200
//);

//Console.WriteLine($"\n\n📊 Stats: {fullResult.WordCount} words, {fullResult.TokenCount} tokens, {fullResult.ElapsedMs}ms");
Console.WriteLine("✅ Response complete!");