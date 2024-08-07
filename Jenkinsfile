def getRepoURL() {
  bat "git config --get remote.origin.url > .git/remote-url"
  return readFile(".git/remote-url").trim()
}

def getCommitSha() {
  bat "git rev-parse HEAD > .git/current-commit"
  return readFile(".git/current-commit").trim()
}

def updateGithubCommitStatus(build) {
  // workaround https://issues.jenkins-ci.org/browse/JENKINS-38674
  repoUrl = getRepoURL()
  commitSha = getCommitSha()

  step([
    $class: 'GitHubCommitStatusSetter',
    reposSource: [$class: "ManuallyEnteredRepositorySource", url: repoUrl],
    commitShaSource: [$class: "ManuallyEnteredShaSource", sha: commitSha],
    errorHandlers: [[$class: 'ShallowAnyErrorHandler']],
    statusResultSource: [
      $class: 'ConditionalStatusResultSource',
      results: [
        [$class: 'BetterThanOrEqualBuildResult', result: 'SUCCESS', state: 'SUCCESS', message: build.description],
        [$class: 'BetterThanOrEqualBuildResult', result: 'FAILURE', state: 'FAILURE', message: build.description],
        [$class: 'AnyBuildResult', state: 'FAILURE', message: 'Loophole']
      ]
    ]
  ])
}

pipeline {
    agent any
	environment {
		WEBHOOK = credentials('discord_webhook_url')
		RELEASE_WEBHOOK = credentials('discord_release_webhook_url')
    GIT_CRED = credentials('BPH TU-Wien Github AccessToken')
    NUGET_PATH = credentials('NUGET_PATH')
	}
    stages {
    	stage ('Build') {
    	    steps {
     	        bat "\"${tool 'VS 2022'}\\msbuild\" SIMULTAN.sln /t:rebuild /restore /p:RestorePackagesConfig=True /p:Configuration=Release /p:Platform=\"Any CPU\""
    	    }
    	}
    	stage ('Test') {
    	    steps {
    	        vsTest testFiles: 'SIMULTAN.Tests\\bin\\Release\\net7.0\\SIMULTAN.Tests.dll'
    	    }
    	}
      /* Don't build nuget for now, settings not finalized
      stage ('Build nuget package') {
        steps {
          bat '%NUGET_PATH% pack -Properties Configuration=Release .\\SIMULTAN\\'
        }
      }
      */
      /* Don't build docs for now
      stage ('Build docs') {
        steps {
          bat 'docfx .\\docfx_project\\docfx.json -o .\\docfx_project\\'
        }
      }
      */
      stage ('Release checks') {
            when {
              allOf {
                expression {
                    currentBuild.result == null || currentBuild.result == 'SUCCESS'
                };
                branch "main" 
              }
            }
            environment {
              REPO_NAME = "SIMULTAN"
            }
            stages {
              stage ('GitHub Release') {
                steps {
                  script {
                    env.STATUS = powershell (
                      script: ".\\make_release.ps1",
                      returnStatus: true
                    )
                  }
                }
              }
              stage ('Notify release') {
                when {
                  expression {
                    env.STATUS == '0' && (currentBuild.result  == null || currentBuild.result == 'SUCCESS')
                  }
                }
                steps {
                  discordSend description: '', successful: currentBuild.resultIsBetterOrEqualTo('SUCCESS'), result: currentBuild.currentResult, title: 'New public version of the Simultan Datamodel was released!', webhookURL: "${RELEASE_WEBHOOK}"
                }
              }
            }
        }
    }
    post {
        always {
            mstest testResultsFile: 'TestResults/*.trx'
            discordSend description: '**Branch:** ' + "${BRANCH_NAME}" + """
**Status:** """ + currentBuild.currentResult, successful: currentBuild.resultIsBetterOrEqualTo('SUCCESS'), link: env.BUILD_URL, result: currentBuild.currentResult, title: 'SIMULTAN public build finished', webhookURL: "${WEBHOOK}"
            updateGithubCommitStatus(currentBuild)
        }
    }
}