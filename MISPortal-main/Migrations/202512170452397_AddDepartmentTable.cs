namespace MisProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDepartmentTable : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AspNetUsers", "DepartmentId", "dbo.Departments");
            DropPrimaryKey("dbo.Departments");
            AddColumn("dbo.Employees", "Department_DepartmentId", c => c.Int());
            AddColumn("dbo.Departments", "DepartmentId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Departments", "DepartmentName", c => c.String(nullable: false, maxLength: 100));
            AddColumn("dbo.Departments", "DepartmentCode", c => c.String(nullable: false, maxLength: 10));
            AddColumn("dbo.Departments", "Description", c => c.String(maxLength: 500));
            AddColumn("dbo.Departments", "CreatedDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.Departments", "ModifiedDate", c => c.DateTime());
            AddColumn("dbo.Departments", "IsActive", c => c.Boolean(nullable: false));
            AddPrimaryKey("dbo.Departments", "DepartmentId");
            CreateIndex("dbo.Employees", "Department_DepartmentId");
            AddForeignKey("dbo.Employees", "Department_DepartmentId", "dbo.Departments", "DepartmentId");
            AddForeignKey("dbo.AspNetUsers", "DepartmentId", "dbo.Departments", "DepartmentId");
            DropColumn("dbo.Departments", "Id");
            DropColumn("dbo.Departments", "Name");
            DropColumn("dbo.Departments", "Code");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Departments", "Code", c => c.String());
            AddColumn("dbo.Departments", "Name", c => c.String());
            AddColumn("dbo.Departments", "Id", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.AspNetUsers", "DepartmentId", "dbo.Departments");
            DropForeignKey("dbo.Employees", "Department_DepartmentId", "dbo.Departments");
            DropIndex("dbo.Employees", new[] { "Department_DepartmentId" });
            DropPrimaryKey("dbo.Departments");
            DropColumn("dbo.Departments", "IsActive");
            DropColumn("dbo.Departments", "ModifiedDate");
            DropColumn("dbo.Departments", "CreatedDate");
            DropColumn("dbo.Departments", "Description");
            DropColumn("dbo.Departments", "DepartmentCode");
            DropColumn("dbo.Departments", "DepartmentName");
            DropColumn("dbo.Departments", "DepartmentId");
            DropColumn("dbo.Employees", "Department_DepartmentId");
            AddPrimaryKey("dbo.Departments", "Id");
            AddForeignKey("dbo.AspNetUsers", "DepartmentId", "dbo.Departments", "Id");
        }
    }
}
