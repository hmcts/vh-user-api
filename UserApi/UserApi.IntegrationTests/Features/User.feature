Feature: User
	In order to manage to ad users
	As an api service
	I want to be able to retrieve or create ad users

Scenario: Create a new hearings reforms user account
	Given I have a new hearings reforms user account request for a new user
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user should be added

Scenario: User account not created for an existing user
	Given I have a new hearings reforms user account request for an existing user
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the user should not be added
	And the response message should read 'user already exists'

Scenario: User account not created for an invalid user
	Given I have a new hearings reforms user account request for the user ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the user should not be added
	And the response message should read 'username cannot be empty'

Scenario: Get user by AD user Id
	Given I have a get user by AD user Id request for the user '60c7fae1-8733-4d82-b912-eece8d55d54c'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved

Scenario: User account not retrieved for a nonexistent user
	Given I have a get user by AD user Id request for the user 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the response should be empty
	And the response message should read 'user does not exist'
	
Scenario: User account not retrieved for an invalid user
	Given I have a get user by AD user Id request for the user ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the response message should read 'username cannot be empty'

Scenario: Get user by user principle name
	Given I have a get user by user principle name request for the user principle name 'VirtualRoomAdministrator@hearings.reform.hmcts.net'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved

Scenario: User account not retrieved for a nonexistent user principle name
	Given I have a get user by user principle name request for the user principle name 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the response should be empty
	And the response message should read 'user priniciple name does not exist'

Scenario: User account not retrieved for an invalid user principle name
	Given I have a get user by user principle name request for the user principle name ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the response message should read 'user principle name cannot be empty'

Scenario: Get user profile by email
	Given I have a get user profile by email request for the email 'VirtualRoomAdministrator@kinley.com'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved

Scenario: User account not retrieved with a nonexistent email
	Given I have a get user profile by email request for the email 'DoesNot@Exist.com'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the response should be empty
	And the response message should read 'email does not exist'

Scenario: User account not retrieved with an incorrectly formatted email
	Given I have a get user profile by email request for the email 'Incorrect format'
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the response message should read 'email has incorrect format'

Scenario: User account not retrieved with an invalid email
	Given I have a get user profile by email request for the email ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the response message should read 'email cannot be empty'