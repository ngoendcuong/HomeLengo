using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HomeLengo.Models;

public partial class Homelengov2Context : DbContext
{
    public Homelengov2Context()
    {
    }

    public Homelengov2Context(DbContextOptions<Homelengov2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminAudit> AdminAudits { get; set; }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogComment> BlogComments { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<ContactU> ContactUs { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Faq> Faqs { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Feature> Features { get; set; }

    public virtual DbSet<Inquiry> Inquiries { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Neighborhood> Neighborhoods { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyAmenity> PropertyAmenities { get; set; }

    public virtual DbSet<PropertyFeature> PropertyFeatures { get; set; }

    public virtual DbSet<PropertyFloorPlan> PropertyFloorPlans { get; set; }

    public virtual DbSet<PropertyPhoto> PropertyPhotos { get; set; }

    public virtual DbSet<PropertyStatus> PropertyStatuses { get; set; }

    public virtual DbSet<PropertyTag> PropertyTags { get; set; }

    public virtual DbSet<PropertyType> PropertyTypes { get; set; }

    public virtual DbSet<PropertyVideo> PropertyVideos { get; set; }

    public virtual DbSet<PropertyVisit> PropertyVisits { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<ServicePlan> ServicePlans { get; set; }

    public virtual DbSet<ServicePlanFeature> ServicePlanFeatures { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserServicePackage> UserServicePackages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-KPR4061M;Database=HOMELENGOV2;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AdminAud__A17F2398378BF331");

            entity.ToTable("AdminAudit");

            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TargetId).HasMaxLength(200);
            entity.Property(e => e.TargetTable).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.AdminAudits)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AdminAudit_Users");
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.AgentId).HasName("PK__Agents__9AC3BFF1DB899357");

            entity.Property(e => e.AgencyName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LicenseNumber).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Agents)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Agents_Users");
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.HasKey(e => e.AmenityId).HasName("PK__Amenitie__842AF50B109EB043");

            entity.HasIndex(e => e.Name, "UQ__Amenitie__737584F6E51F4639").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.BlogId).HasName("PK__Blogs__54379E304AD76FD7");

            entity.HasIndex(e => e.CategoryId, "IDX_Blogs_Category");

            entity.HasIndex(e => e.PublishedAt, "IDX_Blogs_PublishedAt");

            entity.HasIndex(e => e.Slug, "IDX_Blogs_Slug");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.Slug).HasMaxLength(350);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Thumbnail).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.Author).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK_Blogs_Author");

            entity.HasOne(d => d.Category).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Blogs_Category");
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__BlogCate__19093A0B1FEA6649");

            entity.HasIndex(e => e.Name, "UQ__BlogCate__737584F6C52AED8D").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(255);
        });

        modelBuilder.Entity<BlogComment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__BlogComm__C3B4DFCA0CE3DFF7");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsApproved).HasDefaultValue(false);

            entity.HasOne(d => d.Blog).WithMany(p => p.BlogComments)
                .HasForeignKey(d => d.BlogId)
                .HasConstraintName("FK_BlogComments_Blog");

            entity.HasOne(d => d.User).WithMany(p => p.BlogComments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_BlogComments_User");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Bookings__73951AEDB7606311");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.Agent).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_Bookings_Agent");

            entity.HasOne(d => d.Property).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Property");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_User");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21B7690FB2AE5");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ContactU>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PK__ContactU__5C66259B3C7A518D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Information).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa xử lý");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.DistrictId).HasName("PK__District__85FDA4C6A5ED4C49");

            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.City).WithMany(p => p.Districts)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Districts_Cities");
        });

        modelBuilder.Entity<Faq>(entity =>
        {
            entity.HasKey(e => e.FaqId).HasName("PK__FAQs__F6C1B8E5");

            entity.ToTable("FAQs");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Question).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAD55B272E2A");

            entity.HasIndex(e => new { e.UserId, e.PropertyId }, "UX_Favorites_User_Property").IsUnique();

            entity.Property(e => e.AddedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Property).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_Favorites_Properties");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Favorites_Users");
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.FeatureId).HasName("PK__Features__82230BC940AB2A78");

            entity.HasIndex(e => e.Name, "UQ__Features__737584F650351D67").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Inquiry>(entity =>
        {
            entity.HasKey(e => e.InquiryId).HasName("PK__Inquirie__05E6E7CF32B45FCC");

            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactName).HasMaxLength(255);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("new");

            entity.HasOne(d => d.Property).WithMany(p => p.Inquiries)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inquiries_Property");

            entity.HasOne(d => d.User).WithMany(p => p.Inquiries)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Inquiries_User");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PK__Menus__C99ED2307EF03929");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IconClass).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Menus_Parent");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C0C9CD23299C0");

            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Subject).HasMaxLength(255);

            entity.HasOne(d => d.FromUser).WithMany(p => p.MessageFromUsers)
                .HasForeignKey(d => d.FromUserId)
                .HasConstraintName("FK_Messages_FromUser");

            entity.HasOne(d => d.Property).WithMany(p => p.Messages)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_Messages_Property");

            entity.HasOne(d => d.ToUser).WithMany(p => p.MessageToUsers)
                .HasForeignKey(d => d.ToUserId)
                .HasConstraintName("FK_Messages_ToUser");
        });

        modelBuilder.Entity<Neighborhood>(entity =>
        {
            entity.HasKey(e => e.NeighborhoodId).HasName("PK__Neighbor__268014691DDBB4BB");

            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.District).WithMany(p => p.Neighborhoods)
                .HasForeignKey(d => d.DistrictId)
                .HasConstraintName("FK_Neighborhoods_Districts");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E120C5821FC");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3846EAC227");

            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.ProviderRef).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("initiated");

            entity.HasOne(d => d.Transaction).WithMany(p => p.Payments)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("FK_Payments_Transactions");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("PK__Properti__70C9A7357288D365");

            entity.ToTable(tb => tb.HasTrigger("TRG_Properties_UpdateModifiedAt"));

            entity.HasIndex(e => e.CityId, "IDX_Properties_City");

            entity.HasIndex(e => new { e.CityId, e.Price }, "IDX_Properties_City_Price");

            entity.HasIndex(e => e.Price, "IDX_Properties_Price");

            entity.HasIndex(e => e.Title, "IDX_Properties_Title_FullText");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Area).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.LotSize).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Slug).HasMaxLength(350);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Views).HasDefaultValue(0);

            entity.HasOne(d => d.Agent).WithMany(p => p.Properties)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_Properties_Agents");

            entity.HasOne(d => d.City).WithMany(p => p.Properties)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Properties_Cities");

            entity.HasOne(d => d.District).WithMany(p => p.Properties)
                .HasForeignKey(d => d.DistrictId)
                .HasConstraintName("FK_Properties_Districts");

            entity.HasOne(d => d.Neighborhood).WithMany(p => p.Properties)
                .HasForeignKey(d => d.NeighborhoodId)
                .HasConstraintName("FK_Properties_Neighborhoods");

            entity.HasOne(d => d.PropertyType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.PropertyTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_PropertyTypes");

            entity.HasOne(d => d.Status).WithMany(p => p.Properties)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_Status");
        });

        modelBuilder.Entity<PropertyAmenity>(entity =>
        {
            entity.HasKey(e => e.PropertyAmenityId).HasName("PK__Property__0D6BB45E0088FBA9");

            entity.HasOne(d => d.Amenity).WithMany(p => p.PropertyAmenities)
                .HasForeignKey(d => d.AmenityId)
                .HasConstraintName("FK_PropertyAmenities_Amenity");

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyAmenities)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_PropertyAmenities_Property");
        });

        modelBuilder.Entity<PropertyFeature>(entity =>
        {
            entity.HasKey(e => e.PropertyFeatureId).HasName("PK__Property__6B73C3A66C0BCB4D");

            entity.HasIndex(e => e.PropertyId, "IDX_PropertyFeatures_Property");

            entity.Property(e => e.Value).HasMaxLength(255);

            entity.HasOne(d => d.Feature).WithMany(p => p.PropertyFeatures)
                .HasForeignKey(d => d.FeatureId)
                .HasConstraintName("FK_PropertyFeatures_Features");

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyFeatures)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_PropertyFeatures_Properties");
        });

        modelBuilder.Entity<PropertyFloorPlan>(entity =>
        {
            entity.HasKey(e => e.FloorPlanId);

            entity.ToTable("PropertyFloorPlan");

            entity.HasIndex(e => e.PropertyId, "IX_PropertyFloorPlan_PropertyId");

            entity.Property(e => e.Area).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FloorName).HasMaxLength(100);
            entity.Property(e => e.ImagePath).HasMaxLength(1000);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyFloorPlans)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_PropertyFloorPlan_Property");
        });

        modelBuilder.Entity<PropertyPhoto>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PK__Property__21B7B5E28C6A6DE8");

            entity.Property(e => e.AltText).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyPhotos)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_Photos_Properties");
        });

        modelBuilder.Entity<PropertyStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Property__C8EE2063C977E2D5");

            entity.HasIndex(e => e.Name, "UQ__Property__737584F6495038DA").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<PropertyTag>(entity =>
        {
            entity.HasKey(e => e.PropertyTagId).HasName("PK__Property__902DC877EC5DFF28");

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyTags)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_PropertyTags_Property");

            entity.HasOne(d => d.Tag).WithMany(p => p.PropertyTags)
                .HasForeignKey(d => d.TagId)
                .HasConstraintName("FK_PropertyTags_Tag");
        });

        modelBuilder.Entity<PropertyType>(entity =>
        {
            entity.HasKey(e => e.PropertyTypeId).HasName("PK__Property__BDE14DB420A3F537");

            entity.HasIndex(e => e.Name, "UQ__Property__737584F69BD031F3").IsUnique();

            entity.Property(e => e.IconClass).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<PropertyVideo>(entity =>
        {
            entity.HasKey(e => e.VideoId);

            entity.ToTable("PropertyVideo");

            entity.HasIndex(e => e.PropertyId, "IX_PropertyVideo_PropertyId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.VideoType).HasMaxLength(50);
            entity.Property(e => e.VideoUrl).HasMaxLength(1000);

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyVideos)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_PropertyVideo_Property");
        });

        modelBuilder.Entity<PropertyVisit>(entity =>
        {
            entity.HasKey(e => e.VisitId).HasName("PK__Property__4D3AA1DE0FF24A86");

            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.VisitedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.VisitorIp).HasMaxLength(50);

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyVisits)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropertyVisits_Property");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Reports__D5BD4805F391AC32");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReportType).HasMaxLength(100);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CEAE69C071");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsApproved).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Property).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_Reviews_Property");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Reviews_User");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1ADECAA8F2");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61602A59AB20").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("PK__SearchHi__21C535F49F38913D");

            entity.ToTable("SearchHistory");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.QueryText).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_SearchHistory_User");
        });

        modelBuilder.Entity<ServicePlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__ServiceP__755C22B73326D4CF");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<ServicePlanFeature>(entity =>
        {
            entity.HasKey(e => e.FeatureId).HasName("PK__ServiceP__82230BC94C80C521");

            entity.HasIndex(e => new { e.PlanId, e.DisplayOrder }, "IX_ServicePlanFeatures_PlanId_DisplayOrder");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FeatureText).HasMaxLength(255);
            entity.Property(e => e.IsIncluded).HasDefaultValue(true);

            entity.HasOne(d => d.Plan).WithMany(p => p.ServicePlanFeatures)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_ServicePlanFeatures_ServicePlans");
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__Settings__01E719AC8B06C15D");

            entity.Property(e => e.SettingKey).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9AC9E194D9D");

            entity.HasIndex(e => e.Name, "UQ__Tags__737584F667C3D6A1").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A6B4AE27D44");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("pending");
            entity.Property(e => e.TransactionType).HasMaxLength(50);

            entity.HasOne(d => d.Booking).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Transactions_Booking");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA2176C3E");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4BED28FAD").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534C543B9BE").IsUnique();

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK__UserRole__3D978A35FF5D1B6D");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "UX_UserRole_User_Role").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserRoles_Users");
        });

        modelBuilder.Entity<UserServicePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserServicePackages__Id");

            entity.HasIndex(e => e.IsActive, "IX_UserServicePackages_IsActive");

            entity.HasIndex(e => e.UserId, "IX_UserServicePackages_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Plan).WithMany(p => p.UserServicePackages)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServicePackages_Plan");

            entity.HasOne(d => d.User).WithMany(p => p.UserServicePackages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServicePackages_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
