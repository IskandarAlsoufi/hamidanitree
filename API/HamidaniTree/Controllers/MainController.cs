using HamidaniTree.Model;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HamidaniTree.Controllers
{
    //[Authorize]
    [ApiController]
    [EnableCors("hamidani")]
    [Route("service")]
    public class MainController : ControllerBase
    {
        public IDbContextFactory<AppDbContext> AppDbContext { get; }

        public MainController(IDbContextFactory<AppDbContext> AppDbContext,
                                  ILogger<MainController> logger,
                                  IConfiguration config)
        {
            this.AppDbContext = AppDbContext;
        }

        [HttpGet("host")]
        public ActionResult CheckConnection()
        {
            return Ok();
        }

        [HttpGet("tree")]
        public async Task<ActionResult> Tree(string token = "")
        {
            //return tree of people
            using (var mainctx = AppDbContext.CreateDbContext())
            {
                //check token in autho table
                var autho = string.IsNullOrEmpty(token) ? null :
                                                          await mainctx.Authontications.Where(x => x.Token == token)
                                                                                       .Select(x => new
                                                                                       {
                                                                                           x.ID,
                                                                                           x.ExpireDate,
                                                                                           x.Person.ParentLeft,
                                                                                           x.Person.ParentRight,
                                                                                       })
                                                                                       .FirstOrDefaultAsync();
                var tree = await mainctx.People.Include(x => x.Parent).Select(x => new
                {
                    x.ID,
                    x.Name,
                    x.BirthDate,
                    x.DeathDate,
                    x.City,
                    x.Gender,
                    x.Phone,
                    x.FacebookUri,
                    x.IsDead,
                    x.ParentID,
                    x.ParentLeft,
                    x.ParentRight,
                }).ToListAsync();
                return Ok(tree.Select(x => new
                {
                    x.ID,
                    x.Name,
                    x.BirthDate,
                    x.DeathDate,
                    x.City,
                    x.Gender,
                    x.Phone,
                    x.FacebookUri,
                    x.IsDead,
                    x.ParentID,
                    CanEdit = autho == null ? false : (autho.ParentLeft <= x.ParentLeft && autho.ParentRight >= x.ParentRight),
                    Age = CalculateAge(x.BirthDate, x.DeathDate),
                }));
            }
        }

        public static string CalculateAge(DateTime birthDate, DateTime? deathDate = null)
        {
            DateTime currentDate = DateTime.Now;
            int birthYear = birthDate.Year;
            int deathYear = deathDate.HasValue ? deathDate.Value.Year : currentDate.Year;
            int age = deathYear - birthYear;

            if (deathDate.HasValue && (deathDate.Value.Month < birthDate.Month || (deathDate.Value.Month == birthDate.Month && deathDate.Value.Day < birthDate.Day)))
            {
                age--;
            }

            if (age < 1)
            {
                int months = (currentDate.Year - birthDate.Year) * 12 + currentDate.Month - birthDate.Month;
                return $"{months} months";
            }
            else if (age < 5)
            {
                int months = (currentDate.Year - birthDate.Year) * 12 + currentDate.Month - birthDate.Month;
                int years = months / 12;
                months %= 12;
                if (months == 0)
                {
                    return $"{years} years";
                }
                return $"{months} months, {years} years";
            }

            return $"{age} years";
        }

        [HttpPost("new")]
        public async Task<ActionResult> New(PersonDTO person, string token)
        {
            using (var mainctx = AppDbContext.CreateDbContext())
            {
                //check token in autho table
                var autho = await mainctx.Authontications.Where(x => x.Token == token)
                                                     .Select(x => new
                                                     {
                                                         x.ID,
                                                         x.ExpireDate,
                                                         x.Person.ParentLeft,
                                                         x.Person.ParentRight,
                                                     })
                                                     .FirstOrDefaultAsync();

                if (autho == null || autho.ExpireDate < DateTime.Now)
                {
                    return BadRequest("Unauthorized");
                }

                if (person == null)
                {
                    return BadRequest("Invalid Instance!");
                }

                //check if parent exists, if parent is null check if this person is the first person in the tree
                if (person.ParentID == null && await mainctx.People.CountAsync() > 0)
                {
                    return BadRequest("Invalid Parent!");
                }

                var parent = await mainctx.People.Where(x => x.ID == person.ParentID).FirstOrDefaultAsync();
                if (parent == null)
                {
                    return BadRequest("Parent not exist!");
                }

                //check autho person is parent of person
                if (autho.ParentLeft > parent.ParentLeft || autho.ParentRight < parent.ParentRight)
                {
                    return BadRequest("Unauthorized");
                }
                //check properties and return error if any
                if (string.IsNullOrWhiteSpace(person.Name) || person.Name.Length < 2)
                {
                    return BadRequest("Invalid Name!");
                }

                person.Name = person.Name.Trim();

                if (person.BirthDate == System.DateTime.MinValue || (DateTime.Now - person.BirthDate).TotalDays < 1)
                {
                    return BadRequest("Invalid Birth Date!");
                }

                //check if person already exists by name in tree
                if (person.ParentID != null && await mainctx.People.AnyAsync(x => x.ParentID == person.ParentID.Value && x.Name.ToLower() == person.Name.ToLower()))
                {
                    return BadRequest("Person already exists!");
                }

                //check existing password
                if (!string.IsNullOrEmpty(person.Password) && ((person.Password.Length < 4 || person.Password.Length > 10)))
                {
                    return BadRequest("Invalid Password!");
                }

                //check image length and type
                //byte[] thumb = null;
                //if (person.Photo != null)
                //{
                //    thumb = person.Photo.CreateThumbnail(128);
                //    if (thumb.Length > 6000)
                //    {
                //        return BadRequest("Image too Large");
                //    }
                //}

                //person.Photo = thumb;

                //insert person

                try
                {
                    await mainctx.Database.BeginTransactionAsync();
                    var newPerson = new Person()
                    {
                        Name = person.Name,
                        BirthDate = person.BirthDate,
                        DeathDate = person.DeathDate,
                        ParentID = person.ParentID,
                        //Photo = person.Photo,
                        City = person.City,
                        FacebookUri = person.FacebookUri,
                        Gender = person.Gender,
                        IsDead = person.IsDead,
                        Password = person.Password,
                        Phone = person.Phone,
                    };
                    await mainctx.AddNode(newPerson);
                    await mainctx.SaveChangesAsync();
                    await mainctx.Database.CommitTransactionAsync();
                    return Ok(newPerson.ID);
                }
                catch (Exception ex)
                {
                    await mainctx.Database.RollbackTransactionAsync();
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult> Update(PersonDTO person, string token)
        {
            using (var mainctx = AppDbContext.CreateDbContext())
            {
                //check token in autho table
                var autho = await mainctx.Authontications.Where(x => x.Token == token)
                                                         .Select(x => new
                                                         {
                                                             x.ID,
                                                             x.ExpireDate,
                                                             x.Person.ParentLeft,
                                                             x.Person.ParentRight,
                                                         })
                                                         .FirstOrDefaultAsync();

                if (autho == null || autho.ExpireDate < DateTime.Now)
                {
                    return BadRequest("Unauthorized");
                }

                if (person == null || person.ID == 0)
                {
                    return BadRequest("Invalid Instance!");
                }
                //load person
                var dbPerson = await mainctx.People.FirstOrDefaultAsync(x => x.ID == person.ID);
                if (dbPerson == null)
                {
                    return BadRequest("Person not found!");
                }

                //check autho person is parent of person
                if (autho.ParentLeft > dbPerson.ParentLeft || autho.ParentRight < dbPerson.ParentRight)
                {
                    return BadRequest("Unauthorized");
                }

                //check properties and return error if any
                if (string.IsNullOrWhiteSpace(person.Name) || person.Name.Length < 2)
                {
                    return BadRequest("Invalid Name!");
                }

                person.Name = person.Name.Trim();

                if (person.BirthDate != System.DateTime.MinValue && (DateTime.Now - person.BirthDate).TotalDays < 1)
                {
                    return BadRequest("Invalid Birth Date!");
                }

                //check if parent exists, if parent is null check if this person is the first person in the tree
                if (person.ParentID == null && await mainctx.People.CountAsync() > 0)
                {
                    return BadRequest("Invalid Parent!");
                }

                if (person.ParentID != null && !await mainctx.People.AnyAsync(x => x.ID == person.ParentID.Value))
                {
                    return BadRequest("Parent not exist!");
                }

                //check if person already exists by name in tree
                if (person.ParentID != null && await mainctx.People.AnyAsync(x => x.ID != person.ID && x.ParentID == person.ParentID.Value && x.Name.ToLower() == person.Name.ToLower()))
                {
                    return BadRequest("Person already exists!");
                }

                //check existing password
                if (!string.IsNullOrEmpty(person.Password) && ((person.Password.Length < 4 || person.Password.Length > 10) && !await mainctx.People.AnyAsync(x => x.ID != person.ID && x.Password.ToLower() == person.Password.ToLower())))
                {
                    return BadRequest("Invalid Password!");
                }

                //check image length and type
                //byte[] thumb = null;
                //if (person.Photo != null)
                //{
                //    thumb = person.Photo.CreateThumbnail(128);
                //    if (thumb.Length > 6000)
                //    {
                //        return BadRequest("Image too Large");
                //    }
                //}

                //person.Photo = thumb;

                //insert person

                try
                {
                    await mainctx.Database.BeginTransactionAsync();
                    dbPerson.Name = person.Name;
                    dbPerson.BirthDate = person.BirthDate;
                    dbPerson.DeathDate = person.DeathDate;
                    
                    //dbPerson.Photo = person.Photo;
                    dbPerson.City = person.City;
                    dbPerson.FacebookUri = person.FacebookUri;
                    dbPerson.Gender = person.Gender;
                    dbPerson.IsDead = person.IsDead;
                    dbPerson.Phone = person.Phone;
                    if (!string.IsNullOrWhiteSpace(person.Password))
                    {
                        dbPerson.Password = person.Password;
                        mainctx.Entry(dbPerson).Property(x => x.Password).IsModified = true;


                    }
                    //var newPerson = mainctx.People.Find(person.ParentID);
                    //await mainctx.UpdateNode(dbPerson, newPerson);
                    mainctx.Entry(dbPerson).Property(x => x.Name).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.BirthDate).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.DeathDate).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.City).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.FacebookUri).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.Gender).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.IsDead).IsModified = true;
                    mainctx.Entry(dbPerson).Property(x => x.Phone).IsModified = true;
                    await mainctx.SaveChangesAsync();
                    await mainctx.Database.CommitTransactionAsync();
                    return Ok(person.ID);
                }
                catch (Exception ex)
                {
                    await mainctx.Database.RollbackTransactionAsync();
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpDelete("delete")]
        public async Task<ActionResult> Delete(int id, string token)
        {
            using (var mainctx = AppDbContext.CreateDbContext())
            {
                //check token in autho table
                var autho = await mainctx.Authontications.Where(x => x.Token == token)
                                                    .Select(x => new
                                                    {
                                                        x.ID,
                                                        x.ExpireDate,
                                                        x.Person.ParentLeft,
                                                        x.Person.ParentRight,
                                                    })
                                                    .FirstOrDefaultAsync();

                if (autho == null || autho.ExpireDate < DateTime.Now)
                {
                    return BadRequest("Unauthorized");
                }

                //check if person exists
                var person = await mainctx.People.FirstOrDefaultAsync(x => x.ID == id);
                if (person == null)
                {
                    return BadRequest("Person not found!");
                }
                //check autho person is parent of person
                if (autho.ParentLeft > person.ParentLeft || autho.ParentRight < person.ParentRight)
                {
                    return BadRequest("Unauthorized");
                }
                //check if person has children
                if (await mainctx.People.AnyAsync(x => x.ParentID == person.ID))
                {
                    return BadRequest("Person has children!");
                }

                try
                {
                    await mainctx.Database.BeginTransactionAsync();
                    //load parent and update is 
                    var parent = await mainctx.People.FirstOrDefaultAsync(x => x.ID == person.ParentID);    
                    //check token of person who is deleting
                    mainctx.People.Remove(person);
                    await mainctx.SaveChangesAsync();
                    await mainctx.UpdateNode(parent, parent);
                    await mainctx.Database.CommitTransactionAsync();
                    //delete person

                    return Ok();
                }
                catch (Exception ex)
                {
                    await mainctx.Database.RollbackTransactionAsync();
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpGet("autho")]
        public async Task<ActionResult> Autho(int id, string password)
        {
            using (var mainctx = AppDbContext.CreateDbContext())
            {
                //check password
                //get person by password
                var person = await mainctx.People.Where(x =>x.ID == id && x.Password.ToLower() == password.ToLower()).Select(x => new
                {
                    x.ID,
                    x.ParentLeft,
                    x.ParentRight,
                }).FirstOrDefaultAsync();
                if (person == null)
                {
                    return BadRequest("Invalid Password!");
                }

                //create token
                //create autho request
                var autho = new Authontication()
                {
                    PersonID = person.ID,
                    Token = Guid.NewGuid().ToString("N"),
                    ExpireDate = DateTime.Now.AddDays(1)
                };
                mainctx.Authontications.Add(autho);
                await mainctx.SaveChangesAsync();
                //return token
                return Ok(autho.Token);
            }
        }
    }

    public static class Utils
    {
        public static byte[] CreateThumbnail(this byte[] PassedImage, int LargestSide)
        {
            //IL_0018: Unknown result type (might be due to invalid IL or missing references)
            //IL_001e: Expected O, but got Unknown
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            using MemoryStream memoryStream = new MemoryStream();
            using MemoryStream memoryStream2 = new MemoryStream();
            memoryStream.Write(PassedImage, 0, PassedImage.Length);
            Bitmap val = new Bitmap((Stream)memoryStream);
            int num;
            int num2;
            if (((System.Drawing.Image)val).Height > ((System.Drawing.Image)val).Width)
            {
                num = LargestSide;
                num2 = (int)((double)LargestSide / (double)((System.Drawing.Image)val).Height * (double)((System.Drawing.Image)val).Width);
            }
            else
            {
                num2 = LargestSide;
                num = (int)((double)LargestSide / (double)((System.Drawing.Image)val).Width * (double)((System.Drawing.Image)val).Height);
            }

            new Bitmap(num2, num);
            ((System.Drawing.Image)val.ResizeImage(num2, num)).Save((Stream)memoryStream2, ImageFormat.Jpeg);
            return memoryStream2.ToArray();
        }

        private static Bitmap ResizeImage(this Bitmap image, int width, int height)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got Unknown
            //IL_0015: Unknown result type (might be due to invalid IL or missing references)
            //IL_0028: Unknown result type (might be due to invalid IL or missing references)
            Bitmap val = new Bitmap(width, height);
            Graphics val2 = Graphics.FromImage((System.Drawing.Image)(object)val);
            try
            {
                val2.DrawImage((System.Drawing.Image)(object)image, new Rectangle(0, 0, width, height), new Rectangle(0, 0, ((System.Drawing.Image)image).Width, ((System.Drawing.Image)image).Height), (GraphicsUnit)2);
                return val;
            }
            finally
            {
                ((IDisposable)val2)?.Dispose();
            }
        }
    }

    public static class HierarchicalExtensions
    {
        public static async Task<T> AddNode<T>(this AppDbContext context, T entity, bool saveChanges = true) where T : class, IHierarchicalEntity
        {
            if (context == null || entity == null)
            {
                throw new NullReferenceException("entity");
            }

            IHierarchicalEntity<T> entry = entity as IHierarchicalEntity<T>;
            if (entry != null)
            {
                if (!entry.ParentID.HasValue || entry.ParentID == 0)
                {
                    int num = ((await context.Set<T>().CountAsync() != 0) ? await context.Set<T>().Select(l => l.ParentRight).DefaultIfEmpty(0).MaxAsync() : 0);
                    entry.ParentLeft = num + 1;
                    entry.ParentRight = num + 2;
                    await context.Set<T>().AddAsync((T)entry);
                    if (saveChanges)
                    {
                        await context.SaveChangesAsync();
                    }

                    return (T)entry;
                }

                T val = await context.Set<T>().FirstOrDefaultAsync((T c) => (int?)c.ID == entry.ParentID);
                if (val == null)
                {
                    entry.ParentID = 0;
                    return await context.AddNode((T)entry);
                }

                entry.ParentLeft = val.ParentRight;
                entry.ParentRight = val.ParentRight + 1;
                int maxRight = val.ParentRight;
                var source_right = await context.Set<T>().Where(l => l.ParentRight > maxRight - 1).ToArrayAsync();
                foreach (T item in source_right)
                {
                    T current = item;
                    current.ParentRight += 2;
                    context.Entry(current).Property((T l) => l.ParentRight).IsModified = true;
                }
                await context.SaveChangesAsync();

                var source_left = await context.Set<T>().Where(l => l.ParentLeft > maxRight - 1).ToArrayAsync();

                foreach (T item2 in source_left)
                {
                    T current2 = item2;
                    current2.ParentLeft += 2;
                    context.Entry(current2).Property((T l) => l.ParentLeft).IsModified = true;
                }

                await context.SaveChangesAsync();
                await context.Set<T>().AddAsync((T)entry);
                if (saveChanges)
                {
                    await context.SaveChangesAsync();
                }

                return (T)entry;
            }
            return null;
        }

        public static async Task UpdateNode<T>(this AppDbContext context, T entity, T newParent) where T : class, IHierarchicalEntity
        {
            if (context == null || entity == null)
            {
                throw new NullReferenceException("entity");
            }

            IHierarchicalEntity<T> entry = entity as IHierarchicalEntity<T>;
            if (entry == null)
            {
                return;
            }

            if (newParent == null)
            {
                int num = ((await context.Set<T>().CountAsync() != 0) ? await context.Set<T>().Select(l => l.ParentRight).DefaultIfEmpty(0).MaxAsync() : 0);

                var queryable = await context.Set<T>().Where(item => item.ParentLeft > entry.ParentLeft && item.ParentRight < entry.ParentRight).ToArrayAsync();

                int num2 = num - entry.ParentLeft;
                foreach (T item in queryable)
                {
                    T current = item;
                    current.ParentLeft += num2;
                    current.ParentRight += num2;
                    context.Entry(current).Property((T c) => c.ParentLeft).IsModified = true;
                    context.Entry(current).Property((T c) => c.ParentRight).IsModified = true;
                }

                entry.ParentLeft += num2 + 1;
                entry.ParentRight += num2 + 1;
                entry.Parent = null;
                context.Entry(entry).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return;
            }

            if (newParent.ID == 0)
            {
                throw new InvalidOperationException("the new parent not exists in the database");
            }

            List<T> list = await context.Set<T>().Where(item => item.ParentLeft > entry.ParentLeft && item.ParentRight < entry.ParentRight).ToListAsync();

            int num3 = 2 + list.Count() * 2;
            int num4 = newParent.ParentLeft - entry.ParentLeft + 1;
            entry.ParentLeft += num4;
            entry.ParentRight += num4;
            entry.Parent = newParent;
            newParent.ParentRight = entry.ParentRight + 1;
            foreach (T item2 in list)
            {
                T current2 = item2;
                current2.ParentLeft += num4;
                current2.ParentRight += num4;
                context.Entry(current2).Property((T c) => c.ParentLeft).IsModified = true;
                context.Entry(current2).Property((T c) => c.ParentRight).IsModified = true;
            }

            context.Entry(entry).State = EntityState.Modified;
            await context.SaveChangesAsync();
            var source_right = await context.Set<T>().Where(l => l.ParentRight > entry.ParentRight).ToArrayAsync();
            foreach (T item3 in source_right)
            {
                T current3 = item3;
                current3.ParentRight += num3;
                context.Entry(current3).Property((T l) => l.ParentRight).IsModified = true;
            }
            await context.SaveChangesAsync();

            var source_left = await context.Set<T>().Where(l => l.ParentLeft > entry.ParentRight).ToArrayAsync();
            foreach (T item4 in source_left)
            {
                T current4 = item4;
                current4.ParentLeft += num3;
                context.Entry(current4).Property((T l) => l.ParentLeft).IsModified = true;
            }
            await context.SaveChangesAsync();
        }

        public static IQueryable<T> GetNodeChildren<T>(this AppDbContext context, T node) where T : class, IHierarchicalEntity<T>
        {
            if (node == null)
            {
                throw new ArgumentNullException("Node in null");
            }

            return from n in context.Set<T>()
                   where n.ParentLeft >= node.ParentLeft && n.ParentLeft <= node.ParentRight
                   select n;
        }

        public static bool IsChildOf<T>(this IHierarchicalEntity<T> child, T parent) where T : class, IHierarchicalEntity<T>
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            if (child.ParentLeft >= parent.ParentLeft)
            {
                return child.ParentLeft <= parent.ParentRight;
            }

            return false;
        }

        public static bool IsParentOf<T>(this T parent, T child) where T : class, IHierarchicalEntity<T>
        {
            return child.IsChildOf(parent);
        }

        public static IQueryable<T> GetNodeChildren<T>(this AppDbContext context, int nodeID) where T : class, IHierarchicalEntity<T>
        {
            return context.GetNodeChildren(context.Set<T>().Find(nodeID));
        }

        public static IQueryable<T> GetNodeParents<T>(this AppDbContext context, T node) where T : class, IHierarchicalEntity<T>
        {
            if (node == null)
            {
                throw new NotImplementedException("Node in null");
            }

            return from n in context.Set<T>()
                   where n.ParentLeft < node.ParentLeft && n.ParentRight > node.ParentRight
                   select n;
        }

        public static IQueryable<T> GetNodeParents<T>(this AppDbContext context, int nodeID) where T : class, IHierarchicalEntity<T>
        {
            return context.GetNodeParents(context.Set<T>().Find(nodeID));
        }
    }
}