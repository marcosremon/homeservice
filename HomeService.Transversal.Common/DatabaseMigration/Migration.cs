namespace HomeService.Transversal.Common.DatabaseMigration;

public class Migration
{
    public int Version { get; set; }
    public List<string> Commands { get; set; } = [];
}
