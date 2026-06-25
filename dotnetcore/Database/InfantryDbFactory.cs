using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class InfantryDbFactory<TContext> : IDbContextFactory<InfantryDbContext> where TContext : InfantryDbContext
    {
        private readonly IDbContextFactory<TContext> _concreteFactory;
        public InfantryDbFactory(IDbContextFactory<TContext> concreteFactory) => _concreteFactory = concreteFactory;
        public InfantryDbContext CreateDbContext() => _concreteFactory.CreateDbContext();
    }
}
