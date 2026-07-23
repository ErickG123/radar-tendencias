using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace RadarTendencias.Worker.Jobs;

public class MangaRankingItem
{
    public required string Titulo { get; set; }
    public int PosicaoRanking { get; set; }
}

public class AmazonScraperJob
{
    public async Task<List<MangaRankingItem>> ExtrairRankingMangasAsync()
    {
        var resultados = new List<MangaRankingItem>();
        var userDataDir = Path.Combine(Path.GetTempPath(), "playwright_amazon_session");
        
        try
        {
            using var playwright = await Playwright.CreateAsync();
            
            await using var context = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false, // Opção 2: Janela visível para burlar segurança localmente
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

            await Task.Delay(Random.Shared.Next(2000, 5000));
            
            // URL real dos Mais Vendidos em Mangás na Amazon Brasil
            await page.GotoAsync("https://www.amazon.com.br/gp/bestsellers/books/7872782011", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            
            await Task.Delay(Random.Shared.Next(2000, 5000));
            
            // Aguarda o container de produtos aparecer
            await page.WaitForSelectorAsync(".p13n-gridRow, #gridItemRoot", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            // Scroll lento para baixo simulando comportamento humano
            for (int i = 0; i < 5; i++)
            {
                await page.EvaluateAsync("window.scrollBy(0, document.body.scrollHeight / 5)");
                await Task.Delay(Random.Shared.Next(1000, 3000));
            }
            
            var pageHtml = await page.ContentAsync();
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "amazon_dump.html"), pageHtml);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(Path.GetTempPath(), "amazon_screenshot.png") });
            
            var cards = await page.QuerySelectorAllAsync("#gridItemRoot, .zg-grid-general-faceout, .a-carousel-card, [class*='p13n-grid']");
            
            foreach (var card in cards)
            {
                try
                {
                    var titleElement = await card.QuerySelectorAsync("._cDEzb_p13n-sc-css-line-clamp-1_1Fn1y, ._cDEzb_p13n-sc-css-line-clamp-2_EWgoc, div[class*='sc-css-line-clamp']");
                    var positionElement = await card.QuerySelectorAsync(".zg-bdg-text");

                    if (titleElement != null && positionElement != null)
                    {
                        var title = await titleElement.InnerTextAsync();
                        var positionText = await positionElement.InnerTextAsync();
                        
                        var posMatch = Regex.Match(positionText, @"\d+");
                        if (posMatch.Success && int.TryParse(posMatch.Value, out int posicao))
                        {
                            resultados.Add(new MangaRankingItem
                            {
                                Titulo = title.Trim(),
                                PosicaoRanking = posicao
                            });
                        }
                    }
                }
                catch
                {
                    // Ignora itens que mudaram de seletor ou falharam individualmente
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Falha no Scraping da Amazon: " + ex.Message, ex);
        }

        return resultados;
    }
}
