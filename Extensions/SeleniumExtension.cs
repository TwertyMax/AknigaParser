using OpenQA.Selenium;

namespace AudioBooks.Extensions
{
    public static class SeleniumExtension
    {
        public static bool IsElementPresent(this IWebDriver driver, By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }
    }
}
