# 🛡️ Cybersecurity Training Platform

A comprehensive cybersecurity training management system built with ASP.NET Core 9.0, featuring interactive learning modules, progress tracking, and certificate generation.

## 🚀 Features

### Core Functionality
- **👥 User & Group Management**: Complete CRUD operations with many-to-many relationships
- **📚 Module Assignment System**: Assign training modules to groups or individual users
- **🎓 Interactive Training Experience**: Structured lessons with progress tracking
- **📝 Quiz System**: Multiple-choice quizzes with scoring and retake functionality
- **📊 Progress Tracking**: Real-time lesson and module completion monitoring
- **🏆 Certificate Generation**: Downloadable PDF certificates upon module completion

### Admin Features
- **🏢 Company Management**: Multi-tenant company structure
- **👤 User Administration**: User creation, editing, and group assignments
- **👥 Group Management**: Create and manage user groups within companies
- **📖 Content Management**: Create and manage training modules, lessons, and quizzes
- **📋 Assignment Management**: Assign modules to groups or individual users
- **🗄️ Database Management**: Controlled data seeding and management tools

### User Experience
- **🎨 Ultra-Minimalist Design**: Clean, professional interface with consistent styling
- **📱 Responsive Design**: Mobile-first approach that works seamlessly on all devices
- **🎯 Training Dashboard**: Overview of assigned modules with progress indicators
- **📖 Interactive Lessons**: Structured content delivery with progress tracking
- **🧪 Quiz Taking**: Engaging quiz interface with immediate feedback
- **📜 Certificate Viewing**: Professional certificate preview and PDF download

## 🏗️ Technical Architecture

### Technology Stack
- **Backend**: ASP.NET Core 9.0
- **Database**: Entity Framework Core with SQLite
- **Frontend**: Razor Pages with Ultra-Minimalist CSS Framework
- **Authentication**: ASP.NET Core Identity
- **PDF Generation**: iText7 for certificate creation
- **Architecture**: Areas-based admin interface with user portal

### Database Schema
- **Users ↔ Groups**: Many-to-many relationship via UserGroupMemberships
- **Groups → Companies**: Many-to-one relationship  
- **Module Assignments**: Both group-based and direct user assignments
- **Progress Tracking**: Comprehensive tracking of lesson and module progress
- **Quiz Results**: Detailed scoring and answer tracking

### Design System
- **Ultra-Minimalist Framework**: Custom CSS framework for clean, professional appearance
- **Responsive Grid**: Mobile-first approach with flexible layouts
- **Component Library**: Reusable components (stat-cards, info-grids, buttons)
- **Consistent Typography**: Standardized text hierarchy and spacing
- **Clean Tables**: Minimal borders with subtle hover effects for better UX

## 🚦 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/[your-username]/cybersecurity-training-platform.git
   cd cybersecurity-training-platform
   ```

2. **Navigate to the project directory**
   ```bash
   cd CyberSecurityTraining
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Update the database**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - Open your browser and navigate to `http://localhost:5131`
   - Register a new account or use the seeded admin account

### Initial Setup

1. **Seed Sample Data** (Optional)
   - Navigate to `/Admin/SystemAdmin/DatabaseManager`
   - Click "Seed Sample Data" to populate with test content

2. **Create Your First Company**
   - Go to `/Admin/Companies` and create your organization

3. **Set Up User Groups**
   - Navigate to `/Admin/UserGroups` and create training groups

4. **Create Training Content**
   - Add modules at `/Admin/Modules`
   - Create lessons and quizzes for your modules

5. **Assign Training**
   - Use `/Admin/ModuleAssignments` to assign modules to groups

## 🎯 Usage

### For Administrators
1. **Access Admin Panel**: Navigate to `/Admin` (requires admin role)
2. **Manage Companies**: Create and manage organizational structure
3. **User Management**: Create users and assign them to groups
4. **Content Creation**: Build training modules with lessons and quizzes
5. **Assignment Management**: Assign training content to users or groups

### For Users
1. **Training Dashboard**: View assigned modules and progress at `/Training/Dashboard`
2. **Take Training**: Click "Start Module" to begin interactive learning
3. **Complete Lessons**: Progress through lessons at your own pace
4. **Take Quizzes**: Complete assessments to test your knowledge
5. **Download Certificates**: Get PDF certificates for completed modules

## 📁 Project Structure

```
CyberSecurityTraining/
├── Areas/Admin/          # Admin interface pages
├── Pages/Training/       # User training experience
├── Models/              # Entity models and data structures
├── Services/            # Business logic services
├── Data/               # Entity Framework context and migrations
├── wwwroot/            # Static web assets
└── Program.cs          # Application configuration
```

## 🔐 Security Features

- **Authentication**: ASP.NET Core Identity with role-based access
- **Authorization**: Protected admin areas and user-specific content
- **Access Control**: Module assignment validation for training access
- **Data Protection**: Secure handling of user progress and quiz results

## 🛠️ Configuration

### Database Configuration
The application uses SQLite by default. Connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=cybersecurity_training.db"
  }
}
```

### Services Configuration
Key services are registered in `Program.cs`:
- `IProgressService` - Progress tracking and management
- `ICertificateService` - PDF certificate generation
- `IEmailService` - Email notifications (ready for implementation)

## 📈 Current Status

### ✅ Completed Features
- [x] Complete CRUD operations for all entities
- [x] User and group management with many-to-many relationships
- [x] Module assignment system (group and direct assignments)
- [x] Interactive training experience with progress tracking
- [x] Quiz system with scoring and retake functionality
- [x] Certificate generation and PDF download
- [x] Admin interface for complete content management
- [x] Responsive Bootstrap 5 UI
- [x] Comprehensive error handling and validation

### 🚀 Ready for Production
The platform is fully operational and production-ready with all core functionality implemented and tested.

## 🔮 Future Enhancements

- **📊 Advanced Analytics**: Detailed reporting and progress analytics
- **📧 Email Notifications**: Automated assignment and completion notifications
- **⏱️ Timed Quizzes**: Add time limits and advanced quiz features
- **📱 Mobile App**: Native mobile applications for iOS and Android
- **🔗 Integrations**: SCORM compliance and LMS integrations
- **🎨 Theming**: Customizable branding and themes

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Your Name**
- GitHub: [@foosasugaome](https://github.com/foosasugaome)


## 🙏 Acknowledgments

- Built with ASP.NET Core 9.0
- UI powered by Bootstrap 5
- PDF generation using iText7
- Database management with Entity Framework Core

---

**⭐ Star this repository if you find it helpful!**
