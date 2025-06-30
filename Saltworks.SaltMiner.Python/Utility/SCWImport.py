''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-06-30
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

# Use the 'RunSCW.py' code to initialize the code and make sure to define your method in the SCW.json file. 
# The list of available methods begins on line 34 of SCWImport.py

import logging

from Core.RestClient import RestClient

class SCWCode:

    def __init__(self, appSettings,  scwConfigName='SCW'):
        if type(appSettings).__name__ != "ApplicationSettings":
            raise TypeError("Type of appSettings must be 'ApplicationSettings'")

        self.app = appSettings.Application
        logging.info("SCWImport init")

        url = appSettings.Get(scwConfigName, 'Url')
        headers = {
            'accept': 'application/json',
            'X-API-Key': appSettings.Get(scwConfigName, 'ApiKey')
        }
        proxy = appSettings.Get(scwConfigName, 'Proxy', '')
        proxy = None if not proxy else proxy
        proxyUser = appSettings.Get(scwConfigName, 'ProxyUser', '')
        proxyUser = None if not proxyUser else proxyUser
        proxyPass = appSettings.Get(scwConfigName, 'ProxyPassword', '')
        if proxy:
            if proxyUser:
                self.__Client = RestClient(url, defaultHeaders=headers, proxy=proxy, proxyUser=proxyUser, proxyPass=proxyPass)
            else:
                self.__Client = RestClient(url, defaultHeaders=headers, proxy=proxy)
        # end if proxy
        self.__Es = self.app.GetElasticClient()
        self.method = appSettings.Get (scwConfigName, 'Method')
        self.query =  { "query": { "match_all": {} } }
        self.courses_progress_sent = 0
        self.tournament_ranking_sent = 0
        self.assessment_attempts_sent= 0 
        self.leaderboard_docs_sent = 0 
        availableMethods = ["all", "DevActivity", "DevProgress","Leaderboard", "Assessments", "AssessmentAttempts", "Tournaments", "TournamentRankings",
                            "TournamentLeaderboard", "Users", "UserEngagement", "UserTopPerformers", "UserTimeSpent", "Courses", 
                            "CourseProgress", "Teams", "TeamEngagement", "TeamLeaderboard"]
        if self.method not in availableMethods:
            raise ValueError("method not allowed, please use one of the available methods -%s", availableMethods)
        


    def methodPicker(self):
        '''
        This allows you to define the function you would like to use within the RunSCW file. 
        Default is set to all which will get you all data.
        Otherwise you need to choose the method name you would like to use and pass it within the method variable of the __init__

        '''
        #all
        if self.method == "all":
            self.getAllSCWData()

        #developer data
        elif self.method == "DevActivity":
            self.getDeveloperActivity()
        elif self.method == "DevProgress":
            self.getDeveloperProgress()
        elif self.method == "Leaderboard":
            self.getLeaderBoard()
        

        #assessment data
        elif self.method == "Assessments":
            self.getAssessments()
        elif self.method == "AssessmentAttempts":
            self.getAllAssessmentAttempts()

        #tournament data
        elif self.method =="Tournaments":
            self.getTournaments()
        elif self.method == "TournamentRankings":
            self.getAllTournamentRankings()
        elif self.method =="TournamentLeaderboard":
            self.getTournamentLeaderboard()

        #user data
        elif self.method == "Users":
            self.getUsers()
        elif self.method =="UserEngagement":
            self.getUserEngagement()
        elif self.method =="UserTopPerformers":
            self.getUserTopPerformers()
        elif self.method == "UserTimeSpent":
            self.getUserTimeSpent()


        #course data
        elif self.method =="Courses":
            self.getCourses()
        elif self.method =="CourseProgress":
            self.getAllCourseProgress()

        #teams data 
        elif self.method=="Teams":
            self.getTeams()
        elif self.method == "TeamEngagement":
            self.getTeamEngagement()
        elif self.method =="TeamLeaderboard":
            self.getTeamLeaderboard()


    def getAllSCWData(self):
        logging.info("Starting SCW import")

        logging.info("Getting Leaderboard")
        self.getLeaderBoard()

        logging.info("Getting Developer Progress")
        self.getDeveloperProgress()

        logging.info("Getting Developer Activity")
        self.getDeveloperActivity()

        logging.info("Getting Teams")
        self.getTeams()

        logging.info("Getting Team Leaderboard")
        self.getTeamLeaderboard()

        logging.info('Getting Team Engagement')
        self.getTeamEngagement()

        logging.info('Getting Assessments and Assessment Attempts')
        self.getAllAssessmentAttempts()
      
        logging.info('Getting Tournaments and Tournament Rankings')
        self.getAllTournamentRankings()

        logging.info('Getting Users')
        self.getUsers()

        logging.info('Getting User engagement')
        self.getUserEngagement()
        
        logging.info('Getting Top Performers')
        self.getUserTopPerformers()

        logging.info('Getting User Time Spent')
        self.getUserTimeSpent()

        logging.info('Getting Courses and Course Progress')
        self.getAllCourseProgress()

        logging.info('Finished')
        
    def getLeaderBoard(self):
        page = 1
        isOn = True
        pageParams = {
            'report_period': 7,
            'page': page
        }

        indexName = 'scw_ranking'

        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)


        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get('/training/developer-leaderboard', queryParams=pageParams)
            # Old way - for reference
            # pageResponse = requests.get(
            #     url=f'{self.url}/training/developer-leaderboard', headers=self.headers, params=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()     
            numPages = jPageResponse['links']['total_pages']

            

            for rank in jPageResponse['leaderboard']:
                
                developerData = rank['developer']
                for challenge in rank['challenges']:
                    
                    developerData['challenge'] = challenge


                    rankDoc = {
                        "_index": indexName,
                        "_source":developerData
                    }
                    docs_to_send.append(rankDoc)

            page += 1

            if len(docs_to_send) >= 1000:
                self.__Es.BulkInsert(docs_to_send)
                self.leaderboard_docs_sent += len(docs_to_send)
                logging.info('Leaderboard - %s docs sent', len(docs_to_send))
                docs_to_send= []

            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                self.leaderboard_docs_sent += len(docs_to_send)
                logging.info('Leaderboard - %s docs sent', len(docs_to_send))
                logging.info('Leaderboard Docs sent -%s', self.leaderboard_docs_sent)
                isOn = False
            else:

                pageParams = {
                    'report_period': 7,
                    'page': page
                }

    def getDeveloperProgress(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        indexName = 'scw_developer_progress'
        
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/training/developers-progress', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()

            numPages = jPageResponse['links']['total_pages']
            docs_to_send= []

            for developer in jPageResponse['developers']:
                if developer['developer']['last_logged_in'] == ' ' or developer['developer']['last_logged_in'] == '':
                    developer['developer']['last_logged_in'] = None
                    
                developerDoc = {
                "_index": indexName,
                "_source":developer
                }
                docs_to_send.append(developerDoc)
                    
            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('Developer progress - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False

            else:
                pageParams = {
                    'page': page
                }


    def getDeveloperActivity(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }

        indexName = 'scw_developer_activity'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/training/developers-activity', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']
            
            docs_to_send = []

            for activity in jPageResponse['activities']:
                
                if activity['challenge'].__contains__('locate_vulnerability') and (activity['challenge']['locate_vulnerability']['max_score'] == 'N/A' or activity['challenge']['locate_vulnerability']['max_score'] == ''):
                    activity['challenge']['locate_vulnerability']['max_score'] = None

                if activity['challenge'].__contains__('identify_solution') and (activity['challenge']['identify_solution']['max_score'] == 'N/A' or activity['challenge']['identify_solution']['max_score'] == ''):
                    activity['challenge']['identify_solution']['max_score'] = None

                if activity['challenge'].__contains__('select_vulnerability') and (activity['challenge']['select_vulnerability']['max_score'] == 'N/A' or activity['challenge']['select_vulnerability']['max_score'] == ''):
                    activity['challenge']['select_vulnerability']['max_score'] = None
                     
                activityDoc = {
                    "_index": indexName,
                    "_source":activity
                }
                docs_to_send.append(activityDoc)
            
            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('Developer activity - %s docs sent', len(docs_to_send))
            if page > numPages:
                isOn = False
            else:
                pageParams = {
                'page': page
                } 

    def getTeams(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_teams'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)
        
        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get('/teams', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            for team in jPageResponse['teams']:
                
                userDoc = {
                    "_index": indexName,
                    "_source":team
                }
                
                docs_to_send.append(userDoc)

            page += 1

            if len(docs_to_send) >= 10:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Teams - %s docs sent', len(docs_to_send))
                docs_to_send = []
            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Teams - %s docs sent', len(docs_to_send))
                isOn = False
            else:
                pageParams = {
                    'page': page
                } 


    def getTeamLeaderboard(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_team_ranking'

        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/training/team-leaderboard', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']
            for rank in jPageResponse['leaderboard']:
                self.__Es.Index(indexName, rank)

            page += 1
            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'page': page
                }

    def getTeamEngagement(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_team_engagement'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/metrics/activity/teams/most-engaged', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['pages']

            docs_to_send = []

            for team in jPageResponse['data']:
                
                userDoc = {
                    "_index": indexName,
                    "_source":team
                }
                
                docs_to_send.append(userDoc)

            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('Team engagement - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'page': page
                } 


    def getAssessments(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_assessments'
        
        self.assessmentIDList= []
        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get('/assessments', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            

            for assessment in jPageResponse['assessments']:
                assessmentID = assessment['_id']
                self.assessmentIDList.append(assessmentID)
                if self.__Es.IndexExists(indexName):
                    self.query = {
                      "query": {"match_phrase": {
                        "assessment_id": assessmentID
                      }
                      }
                    }
                    
                    self.delete_all_by_query(index=indexName, query=self.query)
                    print(f'Assessment Id:{assessmentID}')
                    self.query = { "query": { "match_all": {} } }
                del assessment['_id']
                data ={"assessment_id": assessmentID}
                data.update(assessment)

                assessmentDoc = {
                    "_index": indexName,
                    "_source":data
                }
                docs_to_send.append(assessmentDoc)

            page += 1

            if len(docs_to_send) >= 100:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Assessments - %s docs sent', len(docs_to_send))
                docs_to_send = []
            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Assessments - %s docs sent', len(docs_to_send))
                isOn = False
            else:
                pageParams = {
                    'page': page
                }    


    def getAssessmentAttempts(self, assessmentID):
        page = 1
        isOn = True
        pageParams = {
            'assessment_id': assessmentID,
            'page': page
        }
        
        indexName = 'scw_assessment_attempts'

        if self.__Es.IndexExists(indexName):
            self.query = {
                      "query": {"match_phrase": {
                        "assessment_id": assessmentID
                      }
                      }
                    }
                    
            self.delete_all_by_query(index=indexName, query=self.query)
            print(f'Assessment Id:{assessmentID}')
            self.query = { "query": { "match_all": {} } }
        self.__Es.MapIndex("scw_assessment_attempts", False)

        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get(f'/assessments/{assessmentID}/attempts', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']
            
           
            if len(jPageResponse['attempts']) > 0:
                for attempt in jPageResponse['attempts']:
                    attemptID = attempt['_id']
                    del attempt['_id']

                    data ={"attempt_id": attemptID}
                    data.update(attempt)

                    attemptDoc = {
                    "_index": indexName,
                    "_source":data
                    }

                    docs_to_send.append(attemptDoc)
                
            else:
                logging.info(f'No Attempts for assessment id {assessmentID}')
                page +=1

                if page > numPages:
                    isOn = False
                
                continue

            page += 1
            if len(docs_to_send) >= 100:
                self.__Es.BulkInsert(docs_to_send)
                self.assessment_attempts_sent += len(docs_to_send)
                logging.info('Assessment attempts - %s docs sent', len(docs_to_send))
                docs_to_send = []
            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                self.assessment_attempts_sent += len(docs_to_send)
                logging.info('Assessment attempts - %s docs sent', len(docs_to_send))
                isOn = False
            else:
                pageParams = {
                    'page': page
                }

        
    def getAllAssessmentAttempts(self):
        self.getAssessments()
        for id in self.assessmentIDList:
            self.getAssessmentAttempts(id)
            logging.info('all available attempts sent, moving to next assessment')
        logging.info("All assessment attempts have been sent. Thank you")
        logging.info("Attempt Docs sent -%s", self.assessment_attempts_sent)

    def getTournaments(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_tournaments'
        self.tournamentIdList=[]
        
        while isOn == True:
            pageResponse = self.__Client.Get('/tournaments', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()

            numPages = jPageResponse['links']['total_pages']

            docs_to_send = []
            for tournament in jPageResponse['tournaments']:
                
                tournamentID = tournament['_id']
                data ={"tournament_id": tournamentID}
                data.update(tournament)
                self.tournamentIdList.append(tournamentID)

                if self.__Es.IndexExists(indexName):
                     
                    self.query = {
                      "query": {"match_phrase": {
                        "tournament_id": tournamentID
                      }
                      }
                    }
                    
                    self.delete_all_by_query(index=indexName, query=self.query)
                    print(f"Tournament Id: {tournamentID}")
                    self.query = { "query": { "match_all": {} } }


                del data['_id']
                
                tournamentDoc = {
                    "_index": indexName,
                    "_source":data
                }
                
                docs_to_send.append(tournamentDoc)

            page += 1
            
            self.__Es.BulkInsert(docs_to_send)
            logging.info('Tournament - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'page': page
                }    


    def getTournamentLeaderboard(self, tournamentID):
        page = 1
        isOn = True
        pageParams = {
            'tournament_id': tournamentID,
            'page': page
        }
        
        indexName = 'scw_tournament_leaderboard'

        if self.__Es.IndexExists(indexName):
            self.query = {
                      "query": {"match_phrase": {
                        "tournament_id": tournamentID
                      }
                      }
                    }
            
        
            self.delete_all_by_query(index=indexName, query=self.query)
            print(f"Tournament Id: {tournamentID}")
            self.query = { "query": { "match_all": {} } }

        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get(f'/tournaments/{tournamentID}/leaderboard', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']
            
            
            
            for rank in jPageResponse['leaderboard']:
                
                if len(rank['levels']) == 0:
                    continue

                if rank['developer']['last_logged_in'] == '':
                    rank['developer']['last_logged_in'] = None


                data ={"tournament_id": tournamentID,
                       "total_max_points": rank['max_points'],
                       "total_points": rank['points'],
                       "percent_of_max_points": round(((float(rank['points'])/float(rank['max_points']))*100), 2)
                       }
                data.update(rank)
                data.pop("points")
                data.pop("max_points")

                attemptDoc = {
                "_index": indexName,
                "_source":data
                }
                
                docs_to_send.append(attemptDoc)

            page += 1
            
            if len(docs_to_send)>= 200:
                self.__Es.BulkInsert(docs_to_send)
                self.tournament_ranking_sent += len(docs_to_send)
                logging.info('Tournament leaderboard - %s docs sent', len(docs_to_send))
                docs_to_send = []

            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                self.tournament_ranking_sent += len(docs_to_send)
                logging.info('Tournament leaderboard - %s docs sent', len(docs_to_send))
                
                isOn = False
            else:
                pageParams = {
                    'tournament_id': tournamentID,
                    'page': page
                }


    def getAllTournamentRankings(self):
        self.getTournaments()
        for id in self.tournamentIdList:
            self.getTournamentLeaderboard(id)
            logging.info('Leaderboard sent, moving to next tournament')
        logging.info("All leaderboards have been sent.")
        logging.info('Tournament Ranking Docs sent -%s', self.tournament_ranking_sent)


    def getUsers(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_users'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/users', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            docs_to_send = []

            for user in jPageResponse['users']:
                userDoc = {
                    "_index": indexName,
                    "_source":user
                }
                docs_to_send.append(userDoc)

            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('Tournament rankings - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'page': page
                }   


    def getUserEngagement(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_user_engagement'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/metrics/activity/users/most-engaged', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            docs_to_send = []

            for user in jPageResponse['data']:
                userDoc = {
                    "_index": indexName,
                    "_source":user
                }
                docs_to_send.append(userDoc)

            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('User engagement - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'page': page
                } 
    
    
    def getUserTopPerformers(self):
        page = 1
        isOn = True
        pageParams = {
            'report_period': 7,
            'page': page
        }
        
        indexName = 'scw_user_top_performers'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        while isOn == True:
            pageResponse = self.__Client.Get('/metrics/activity-top-performers', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            docs_to_send = []

            for performer in jPageResponse['top_performers']:
                performerDoc = {
                    "_index": indexName,
                    "_source":performer
                }
                docs_to_send.append(performerDoc)

            page += 1

            self.__Es.BulkInsert(docs_to_send)
            logging.info('User top performers - %s docs sent', len(docs_to_send))

            if page > numPages:
                isOn = False
            else:
                pageParams = {
                    'report_period': 7,
                    'page': page
                    }

    def getUserTimeSpent(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_user_time_spent'
        if self.__Es.IndexExists(indexName):
            self.delete_all_by_query(index=indexName, query=self.query)

        
        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get('/metrics/time-spent', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            

            for user in jPageResponse['users']:

                if len(user['time-spent']) == 0:
                    continue
                
                userName= user['name']
                userId = user['id']
                userEmail = user['email']
                userTeam = user['team']

                for activityDoc in user['time-spent']:
                    activityDate = activityDoc['date']
                    activityDoc['activity']['total'] = sum(activityDoc['activity'].values())
                    

                    timeSpentDoc = {
                        "id": userId,
                        "name": userName,
                        "email": userEmail,
                        "date": activityDate,
                        "activity": activityDoc['activity'],
                        "team": userTeam     
                    }
                    userDoc = {
                        "_index": indexName,
                        "_source":timeSpentDoc
                    }
                    docs_to_send.append(userDoc)

        
            page += 1

            if len(docs_to_send) >= 100:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('User time spent - %s docs sent', len(docs_to_send))
                docs_to_send = []
            
            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('User time spent - %s docs sent', len(docs_to_send))
                isOn = False
            else:
                pageParams = {
                    'page': page
                    }
    

    def getCourses(self):
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        self.courseIdList=[]
        docs_to_send = []
        while isOn == True:
            indexName = 'scw_courses'
            pageResponse = self.__Client.Get('/courses', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']

            

            for course in jPageResponse['data']:
                courseID = course['_id']
                data ={"course_id": courseID}
                data.update(course)
                self.courseIdList.append(courseID)

                if self.__Es.IndexExists(indexName):
                    self.query = {
                      "query": {"match_phrase": {
                        "course_id": courseID
                      }
                      }
                    }
                    
                    self.delete_all_by_query(index=indexName, query=self.query)
                    print(f"Course id: {courseID}")
                    self.query = { "query": { "match_all": {} } }
                del data['_id']
                courseDoc = {
                    "_index": indexName,
                    "_source":data
                }
                docs_to_send.append(courseDoc)

            page += 1

            if len(docs_to_send) >= 200:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Courses - %s docs sent', len(docs_to_send))
                docs_to_send = []
            if page > numPages:
                self.__Es.BulkInsert(docs_to_send)
                logging.info('Courses - %s docs sent', len(docs_to_send))
                isOn = False
            else:
                pageParams = {
                    'page': page
                    }
        
    def getUserCourseProgress(self, courseID):
        
        page = 1
        isOn = True
        pageParams = {
            'page': page
        }
        
        indexName = 'scw_course_user_progress'
        
        if self.__Es.IndexExists(indexName):
            
            self.query = {
                      "query": {"match_phrase": {
                        "course_id": courseID
                      }
                      }
                    }
            
            self.delete_all_by_query(index=indexName, query=self.query)
            print(f"Course id: {courseID}")
            self.query = { "query": { "match_all": {} } }

        docs_to_send = []
        while isOn == True:
            pageResponse = self.__Client.Get(f'/courses/{courseID}/developers-progress', queryParams=pageParams)
            pageResponse.raise_for_status()
            jPageResponse = pageResponse.json()
            numPages = jPageResponse['links']['total_pages']
            
            
            
            for user in jPageResponse['data']:
                data ={"course_id": courseID}
                data.update(user)
                
                courseProgressDoc = {
                "_index": indexName,
                "_source":data
                }
                docs_to_send.append(courseProgressDoc)
                courseProgressDoc={}
                
            page += 1
            
            if len(docs_to_send) >= 2000:
                self.__Es.BulkInsert(docs_to_send)
                self.courses_progress_sent += len(docs_to_send)
                logging.info('User course progress - %s docs sent', len(docs_to_send))
                docs_to_send = []

            if page > numPages:
                if len(docs_to_send) > 0:
                    self.__Es.BulkInsert(docs_to_send)
                    self.courses_progress_sent += len(docs_to_send)
                logging.info('User course progress - %s docs sent', len(docs_to_send))
                
                isOn = False
            else:
                pageParams = {
                    'page': page
                }

    def getAllCourseProgress(self):
        self.getCourses()
        for id in self.courseIdList:
            logging.info("Course progress sending for course %s.", id)
            self.getUserCourseProgress(id)
        logging.info("All courses' progress have been sent.")
        logging.info('Course progress Docs sent - %s', self.courses_progress_sent)


    def delete_all_by_query(self, index, query):
        
        logging.info("Clearing data from index '%s' for reload", index)
        self.__Es.DeleteByQuery(index, query)


    def delete_id_by_query(self, index, id):
        self.__Es.DeleteById(index, id, ignoreMissingIndex=True, ignoreNotFound=True)


