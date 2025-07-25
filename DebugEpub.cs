using VersOne.Epub;

class DebugEpub
{
    static async Task Main(string[] args)
    {
        var epubPath = @"d:\MyLearning\Ky7\SWD\Projects\csharpwebapp-booklify-be\docs\milne-once-a-week.zip";
        
        // Extract epub first
        System.IO.Compression.ZipFile.ExtractToDirectory(epubPath, @"d:\temp\epub-debug");
        epubPath = @"d:\temp\epub-debug\milne-once-a-week\milne-once-a-week.epub";
        
        var epubBook = await EpubReader.ReadBookAsync(epubPath);
        
        Console.WriteLine($"Navigation items count: {epubBook.Navigation?.Count ?? 0}");
        
        if (epubBook.Navigation != null)
        {
            foreach (var navItem in epubBook.Navigation)
            {
                Console.WriteLine($"Title: '{navItem.Title}', Link: '{navItem.Link?.ContentFilePath}', Type: {navItem.Type}");
                
                if (navItem.NestedItems?.Any() == true)
                {
                    foreach (var nested in navItem.NestedItems)
                    {
                        Console.WriteLine($"  - Nested: '{nested.Title}', Link: '{nested.Link?.ContentFilePath}'");
                        
                        if (nested.NestedItems?.Any() == true)
                        {
                            foreach (var nested2 in nested.NestedItems)
                            {
                                Console.WriteLine($"    - Nested2: '{nested2.Title}', Link: '{nested2.Link?.ContentFilePath}'");
                            }
                        }
                    }
                }
            }
        }
        
        Console.WriteLine("\nReading Order items:");
        foreach (var item in epubBook.ReadingOrder)
        {
            Console.WriteLine($"ReadingOrder: '{item.FilePath}'");
        }
    }
}
