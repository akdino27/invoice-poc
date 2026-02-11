# AVEVA Invoice - Angular 21 Frontend

A modern, production-ready Angular 21 application for invoice management with Google OAuth 2.0 authentication and direct Google Drive file uploads.

## 🚀 Features

- ✅ **Google OAuth 2.0 Authentication** - Secure sign-in with Google accounts
- ✅ **Direct Google Drive Upload** - Files upload directly from frontend to Google Drive (no backend upload needed)
- ✅ **File Management** - View, search, download, and delete uploaded files
- ✅ **Modern UI** - Clean, enterprise-grade interface with Tailwind CSS
- ✅ **Standalone Components** - Pure Angular 21 with no NgModules
- ✅ **Drag & Drop** - Easy file upload with drag-and-drop support
- ✅ **Progress Tracking** - Real-time upload progress indicators
- ✅ **Responsive Design** - Mobile-friendly interface

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** (v18.x or higher)
- **npm** (v9.x or higher)
- **Angular CLI** (v21.x)
  ```bash
  npm install -g @angular/cli@21
  ```

## 🔧 Installation

### 1. Clone & Install Dependencies

```bash
cd aveva-invoice
npm install
```

### 2. Google Cloud Setup

#### A. Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Note your Project ID

#### B. Enable Required APIs

1. Go to **APIs & Services > Library**
2. Enable the following APIs:
   - Google Drive API
   - Google+ API (for user info)

#### C. Create OAuth 2.0 Credentials

1. Go to **APIs & Services > Credentials**
2. Click **Create Credentials > OAuth client ID**
3. Choose **Web application**
4. Configure:
   - **Authorized JavaScript origins:**
     ```
     http://localhost:4200
     http://localhost:5247
     https://localhost:7072
     ```
   - **Authorized redirect URIs:**
     ```
     http://localhost:4200
     http://localhost:5247
     https://localhost:7072
     ```
5. Copy the **Client ID** (you'll need this)

#### D. Create Google Drive Shared Folder

1. Go to [Google Drive](https://drive.google.com)
2. Create a new folder (e.g., "AVEVA Invoices")
3. Right-click the folder > **Share**
4. Set sharing permissions as needed
5. Copy the Folder ID from the URL:
   ```
   https://drive.google.com/drive/folders/FOLDER_ID_HERE
   ```

### 3. Configure Environment Variables

Update the following files with your Google credentials:

#### `src/environments/environment.development.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5247',
  google: {
    clientId: 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com', // ← Add your Client ID
    scopes: [
      'https://www.googleapis.com/auth/drive.file',
      'https://www.googleapis.com/auth/drive.readonly',
      'https://www.googleapis.com/auth/userinfo.profile',
      'https://www.googleapis.com/auth/userinfo.email'
    ],
    discoveryDocs: ['https://www.googleapis.com/discovery/v1/apis/drive/v3/rest'],
    sharedFolderId: 'YOUR_SHARED_FOLDER_ID' // ← Add your Folder ID
  }
};
```

#### `src/environments/environment.ts`

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://localhost:7072',
  google: {
    clientId: 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com', // ← Add your Client ID
    scopes: [
      'https://www.googleapis.com/auth/drive.file',
      'https://www.googleapis.com/auth/drive.readonly',
      'https://www.googleapis.com/auth/userinfo.profile',
      'https://www.googleapis.com/auth/userinfo.email'
    ],
    discoveryDocs: ['https://www.googleapis.com/discovery/v1/apis/drive/v3/rest'],
    sharedFolderId: 'YOUR_SHARED_FOLDER_ID' // ← Add your Folder ID
  }
};
```

## 🎯 Running the Application

### Development Server

```bash
ng serve
```

Navigate to `http://localhost:4200/`. The application will automatically reload when you make changes.

### Production Build

```bash
ng build --configuration production
```

The build artifacts will be stored in the `dist/` directory.

## 📁 Project Structure

```
aveva-invoice/
├── src/
│   ├── app/
│   │   ├── core/                      # Core functionality
│   │   │   ├── guards/                # Route guards
│   │   │   │   └── auth.guard.ts
│   │   │   ├── interceptors/          # HTTP interceptors
│   │   │   │   └── auth.interceptor.ts
│   │   │   ├── models/                # Data models
│   │   │   │   ├── drive-file.model.ts
│   │   │   │   └── user.model.ts
│   │   │   └── services/              # Core services
│   │   │       ├── auth.service.ts
│   │   │       ├── drive.service.ts
│   │   │       └── file-upload.service.ts
│   │   ├── features/                  # Feature modules
│   │   │   ├── auth/
│   │   │   │   └── login/
│   │   │   ├── dashboard/
│   │   │   ├── upload/
│   │   │   └── files/
│   │   ├── shared/                    # Shared components
│   │   │   ├── components/
│   │   │   │   ├── layout/
│   │   │   │   ├── sidebar/
│   │   │   │   └── topbar/
│   │   │   └── directives/
│   │   │       └── drag-drop.directive.ts
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.development.ts
│   ├── index.html
│   ├── main.ts
│   └── styles.css
├── angular.json
├── package.json
├── tailwind.config.js
└── README.md
```

## 🔐 Authentication Flow

1. User clicks "Sign in with Google" on login page
2. Google OAuth consent screen appears
3. User grants permissions for Drive and profile access
4. Access token is stored in session storage
5. User is redirected to dashboard
6. Token is automatically included in Drive API requests

## 📤 File Upload Flow

1. User selects or drags files on the Upload page
2. Files are validated (type, size)
3. Each file is uploaded **directly to Google Drive** via Drive API
4. Upload progress is tracked in real-time
5. After upload completes, user is redirected to Files page
6. Backend can process files from Google Drive independently

**Important:** Files are uploaded directly from the frontend to Google Drive. The backend does NOT receive file uploads.

## 🎨 UI Components

### Pages

- **Login** - Google OAuth sign-in
- **Dashboard** - Overview (placeholder for future analytics)
- **Upload Invoice** - Drag & drop file upload interface
- **Files** - Browse, search, download, and delete files

### Layout

- **Sidebar** - Navigation menu
- **Topbar** - User profile and app title
- **Responsive** - Mobile-friendly design

## 🛠️ Development

### Code Scaffolding

Generate a new component:
```bash
ng generate component features/my-component --standalone
```

Generate a new service:
```bash
ng generate service core/services/my-service
```

### Running Unit Tests

```bash
ng test
```

### Linting

```bash
ng lint
```

## 🔒 Security Considerations

1. **Access Token Storage**: Currently stored in `sessionStorage`. Consider using more secure methods for production.
2. **CORS**: Ensure your backend API has proper CORS configuration
3. **Google Drive Permissions**: Only share folder with necessary users
4. **API Scopes**: Only request minimum required scopes
5. **Token Expiration**: Implement token refresh logic for long sessions

## 🌐 API Integration (Backend)

While file uploads go directly to Google Drive, you may want to integrate with your ASP.NET Core backend for:

- Invoice metadata storage
- Processing status tracking
- User management
- Analytics

### Example API Call

```typescript
// In a service
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';

constructor(private http: HttpClient) {}

getInvoiceMetadata(fileId: string) {
  return this.http.get(`${environment.apiUrl}/api/invoices/${fileId}`);
}
```

## 📦 Dependencies

### Main Dependencies

- `@angular/core`: ^21.0.0
- `@angular/common`: ^21.0.0
- `@angular/router`: ^21.0.0
- `tailwindcss`: ^3.4.17
- `rxjs`: ~7.8.0

### External APIs

- Google Identity Services
- Google Drive API v3
- Google OAuth 2.0

## 🚧 Troubleshooting

### Issue: "Access blocked: Authorization Error"

**Solution:** Make sure your OAuth consent screen is configured correctly and the app is in testing mode with your Google account added as a test user.

### Issue: "Origin not allowed"

**Solution:** Add `http://localhost:4200` to Authorized JavaScript origins in Google Cloud Console.

### Issue: Files not appearing in Google Drive

**Solution:** 
1. Verify the `sharedFolderId` is correct
2. Check that your Google account has write access to the folder
3. Verify Drive API is enabled in Google Cloud Console

### Issue: "Failed to load files"

**Solution:**
1. Check browser console for detailed error
2. Verify access token is valid
3. Ensure proper API scopes are requested

## 📝 Future Enhancements

- [ ] Add invoice OCR/extraction
- [ ] Implement dashboard analytics
- [ ] Add batch operations
- [ ] Implement file preview
- [ ] Add invoice categorization
- [ ] Email notifications
- [ ] Advanced search filters
- [ ] Export functionality

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 👥 Support

For issues, questions, or contributions, please open an issue on the repository.

---

**Built with ❤️ using Angular 21 and Google Cloud Platform**
