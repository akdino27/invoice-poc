export const environment = {
  production: false,
  apiUrl: 'http://localhost:5247',
  google: {
    clientId: '9948699749-6sqao0031k188duql8rcen8vrh8kg1o3.apps.googleusercontent.com',
    scopes: [
      'https://www.googleapis.com/auth/drive.file',
      'https://www.googleapis.com/auth/drive.readonly',
      'https://www.googleapis.com/auth/userinfo.profile',
      'https://www.googleapis.com/auth/userinfo.email'
    ],
    discoveryDocs: ['https://www.googleapis.com/discovery/v1/apis/drive/v3/rest'],
    // Replace with your actual Google Drive Shared Folder ID
    sharedFolderId: '1-PenAeLnXGUiZeNrmxJoVOX4yAf_sMCJ'
  }
};
