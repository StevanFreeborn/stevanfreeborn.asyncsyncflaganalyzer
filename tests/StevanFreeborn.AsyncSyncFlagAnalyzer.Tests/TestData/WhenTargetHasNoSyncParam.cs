using System.Threading.Tasks;

public class BusinessLogic
{
  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    await Task.Delay(100);
    return "done";
  }
}
