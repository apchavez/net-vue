// Jenkins Declarative Pipeline equivalent of .github/workflows/ci.yml.
// Kept alongside GitHub Actions (the CI actually enforced on this repo) to demonstrate
// Jenkinsfile/Groovy DSL fluency for orgs that run Jenkins on-prem instead of a SaaS CI.
pipeline {
    agent any

    tools {
        nodejs 'node22'
    }

    environment {
        SONAR_TOKEN = credentials('sonar-token')
    }

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Trivy vulnerability scan') {
            steps {
                sh '''
                    trivy fs --scanners vuln --severity CRITICAL,HIGH \
                        --exit-code 1 --ignore-unfixed --format table .
                '''
            }
        }

        stage('API — SonarCloud + build + coverage') {
            steps {
                dir('api') {
                    sh '''
                        dotnet sonarscanner begin /o:$SONAR_ORGANIZATION /k:apchavez_net-vue \
                            /d:sonar.host.url=https://sonarcloud.io \
                            /d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
                        dotnet restore ProductApi.sln
                        dotnet build ProductApi.sln --no-restore -c Release
                        dotnet test tests/ProductApi.UnitTests --no-build -c Release \
                            --collect:"XPlat Code Coverage;Format=opencover" --results-directory ./coverage
                        dotnet test tests/ProductApi.IntegrationTests --no-build -c Release \
                            --collect:"XPlat Code Coverage;Format=opencover" --results-directory ./coverage
                        dotnet sonarscanner end
                    '''
                }
            }
        }

        stage('Web — lint, format, typecheck, test, build') {
            steps {
                dir('web') {
                    sh '''
                        npm ci
                        npm run lint
                        npm run format:check
                        npx vue-tsc -b
                        npm run test
                        npm run build
                    '''
                }
            }
        }

        stage('Web — E2E Playwright') {
            steps {
                dir('web') {
                    sh '''
                        npx playwright install --with-deps chromium
                        npm run dev -- --port 5173 &
                        npx wait-on http://localhost:5173
                        npm run test:e2e
                    '''
                }
            }
        }

        stage('Validate k8s manifests') {
            steps {
                sh '''
                    helm lint ./chart
                    helm template product-api ./chart --namespace product-api \
                        --set secrets.dbUser=test --set secrets.dbPassword=test \
                        --set secrets.kafkaPassword=test --set secrets.redisPassword=test \
                        | kubeconform -strict -ignore-missing-schemas -summary -
                '''
            }
        }

        stage('Docker build & push') {
            when {
                branch 'main'
            }
            steps {
                sh '''
                    docker build -t ghcr.io/apchavez/net-vue-api:latest ./api
                    docker build -t ghcr.io/apchavez/net-vue-web:latest ./web
                '''
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'api/coverage/**, web/playwright-report/**', allowEmptyArchive: true
        }
    }
}
