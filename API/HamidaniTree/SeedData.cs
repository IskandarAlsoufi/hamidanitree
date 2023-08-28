using HamidaniTree.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using HamidaniTree.Controllers;

namespace HamidaniTree
{
    internal class SeedData
    {
        private AppDbContext context;

        public SeedData(AppDbContext context)
        {
            this.context = context;
        }

        internal async Task Seed()
        {
            //create user with state active
            var newPerson = new Person()
            {
                Name = "Said",
                BirthDate = DateTime.Now.AddDays(-(200*360)),
                Gender = false,
                IsDead = true,
                Password = "hamidani@tree#2023",
               
            };
            await context.AddNode(newPerson);
            await context.SaveChangesAsync();
        }
    }
}