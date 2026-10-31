using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DominoPontaDeQuina.Migrations;

public class DominoDbContextFactory : IDesignTimeDbContextFactory<DominoDbContext>
{
    public DominoDbContext CreateDbContext(string[] args)
    {
        return new DominoDbContext();
    }
}
