using System.Threading.Tasks;

public interface IDataService
{
  Task<string> GetCoreAsync(int id, bool sync = false);
}

public class BusinessLogic
{
  private readonly IDataService _dataService;
  public BusinessLogic(IDataService dataService) { _dataService = dataService; }

  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    return await {|#0:_dataService.GetCoreAsync(17, sync: false)|};
  }
}
