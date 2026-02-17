#!/bin/bash

echo "🚀 Setting up Invoice Processing Frontend..."

# Install dependencies
echo "📦 Installing dependencies..."
npm install

# Create environment files if they don't exist
if [ ! -f "src/environments/environment.ts" ]; then
  echo "📝 Creating environment.ts..."
  mkdir -p src/environments
  cat > src/environments/environment.ts << EOF
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5247/api'
};
EOF
fi

if [ ! -f "src/environments/environment.prod.ts" ]; then
  echo "📝 Creating environment.prod.ts..."
  cat > src/environments/environment.prod.ts << EOF
export const environment = {
  production: true,
  apiUrl: 'https://your-production-api.com/api'
};
EOF
fi

echo "✅ Setup complete!"
echo "🎯 Run 'npm start' to start the development server"
