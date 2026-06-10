repo_structure.md
# Repo Folder Structure

The following is the template for our planned repo structure

repo-root/
├── README.md
├── CONTRIBUTING.md
├── LICENSE
├── .gitignore
├── .github/
│         ├── workflows/
│         ├── ISSUE_TEMPLATE/
│         └── PULL_REQUEST_TEMPLATE/
│
├── docs/
│         ├── README.md
│         ├── architecture/
│         ├── api/
│         ├── guides/
│         └── adr/
│
├── dotnet/
│         ├── README.md
│         ├── global.json
│         ├── Directory.Build.props
│         ├── src/
│         └── tests/
│
├── python/
│         ├── README.md
│         ├── pyproject.toml
│         ├── src/
│         ├── tests/
│         └── scripts/
│
├── frontend/                                                                          # Frontend projects container
│         ├── README.md                              # Overview of both projects
│         │
│         ├── admin-dashboard/                        # First frontend project
│         │         ├── README.md
│         │         ├── package.json
│         │         ├── package-lock.json
│         │         ├── vite.config.js
│         │         ├── .eslintrc.js
│         │         ├── .prettierrc
│         │         ├── public/
│         │         ├── src/
│         │         │         ├── components/
│         │         │         ├── pages/
│         │         │         ├── store/
│         │         │         ├── services/
│         │         │         ├── App.vue
│         │         │         └── main.js
│         │         ├── tests/
│         │         └── dist/
│         │
│         └── public-portal/                                                                 # Second frontend project
│                         ├── README.md
│                         ├── package.json
│                         ├── package-lock.json
│                         ├── vite.config.js
│                         ├── .eslintrc.js
│                         ├── .prettierrc
│                         ├── public/
│                         ├── src/
│                         │         ├── components/
│                         │         ├── pages/
│                         │         ├── store/
│                         │         ├── services/
│                         │         ├── App.vue
│                         │         └── main.js
│                         ├── tests/
│                         └── dist/
│
├── scripts/
│         ├── README.md
│         ├── setup.sh
│         ├── build-all.sh
│         └── run-tests.sh
│
├── docker/
│         ├── README.md
│         ├── Dockerfile.dotnet
│         ├── Dockerfile.python
│         ├── Dockerfile.admin-dashboard
│         ├── Dockerfile.public-portal
│         └── docker-compose.yml
│
├── config/
│         ├── README.md
│         ├── development/
│         ├── staging/
│         └── production/
│
└── .editorconfig


