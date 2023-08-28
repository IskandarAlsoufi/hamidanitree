using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HamidaniTree.Model
{
    [Table("People")]
    public class Person : IHierarchicalEntity<Person>
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity), Column(Order = 0)]
        public  int ID{ get; set; }

        public string Name{ get; set; }

        public DateTime BirthDate { get; set; }

        public DateTime? DeathDate { get; set; }

        public bool IsDead { get; set; }
        
        public bool Gender{ get; set; } 

        /// <summary>
        /// Base64
        /// </summary>
        [MaxLength(6000)]
        public byte[] Photo { get; set; }

        public string Phone { get; set; }
        public string City { get; set; }

        public string FacebookUri { get; set; }

        public DateTime? LastLogin { get; set; }

        [ForeignKey("Parent")]
        public int? ParentID { get; set; }
        public virtual Person Parent { get; set; }

        [Required, MaxLength(20)]
        public string Password { get; set; }
        public int ParentLeft { get; set; }
        public int ParentRight { get; set; }
    }

    [Table("Authontications")]
    [Index(nameof(Token), IsUnique = true)]

    public class Authontication
    {
        public Authontication()
        {
            ExpireDate = DateTime.Now.AddDays(1);
        }
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column(Order = 0)]
        public int ID { get; set; }
        [ForeignKey("Person")]
        public int PersonID { get; set; }
        public virtual Person Person{ get; set; }
        [MaxLength(32)]
        public string Token { get; set; }
        public DateTime ExpireDate { get; set; }
    }


    public class PersonDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime? DeathDate { get; set; }
        public bool IsDead { get; set; }
        public bool Gender { get; set; }

        /// <summary>
        /// Base64
        /// </summary>
        //public byte[] Photo { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string FacebookUri { get; set; }
        public int? ParentID { get; set; }
        public string Password { get; set; }

    }

    public interface IHierarchicalLeftRightParents : IIdentity
    {
        int ParentLeft { get; set; }

        int ParentRight { get; set; }

        new int ID { get; set; }

        int? ParentID { get; set; }
    }
    public interface IIdentity
    {
        int ID { get; set; }
    }
    public interface IHierarchicalEntity : IHierarchicalLeftRightParents, IIdentity
    {
    }
    public interface IHierarchicalEntity<TParent> : IHierarchicalLeftRightParents, IIdentity, IHierarchicalEntity where TParent : class, IHierarchicalLeftRightParents
    {
        TParent Parent { get; set; }
    }
}
