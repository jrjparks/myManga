
using Amib.Threading;
namespace myManga_App.IO.Network
{
    public class SmartChapterDownloader : SmartDownloader
    {
        public SmartChapterDownloader() : base() { }
        public SmartChapterDownloader(STPStartInfo stpThredPool) : base(stpThredPool) { }
    }
}
