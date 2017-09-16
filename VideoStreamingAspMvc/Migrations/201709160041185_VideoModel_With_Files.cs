namespace VideoStreamingAspMvc.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class VideoModel_With_Files : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Videos", "VideoFileName", c => c.String());
            AddColumn("dbo.Videos", "ImageFileName", c => c.String());
            AddColumn("dbo.Videos", "Length", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Videos", "Length");
            DropColumn("dbo.Videos", "ImageFileName");
            DropColumn("dbo.Videos", "VideoFileName");
        }
    }
}
