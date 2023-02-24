Feature: User
	In order to manage to ad users
	As an api service
	I want to be able to retrieve or create ad users

Scenario: User account not created for an invalid user
	Given I have a new hearings reforms user account request with an invalid email
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the error response message should contain 'recovery email cannot be empty'
	And the error response message should also contain 'first name cannot be empty'
	And the error response message should also contain 'last name cannot be empty'

Scenario: User account not created with an incorrectly formatted email
	Given I have a new hearings reforms user account request with an IncorrectFormat email
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the error response message should contain 'email has incorrect format'

Scenario: User account not retrieved for a nonexistent user
	Given I have a get user by AD user Id request for a nonexistent user with Individual
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the error response message should contain 'user does not exist'
	

Scenario: User account not retrieved for a nonexistent user principal name
	Given I have a get user by user principal name request for a nonexistent user principal name
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the error response message should contain 'user principal name does not exist'

Scenario: User account not retrieved for an invalid user principal name
	Given I have a get user by user principal name request for an invalid user principal name
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the error response message should contain 'user principal name cannot be empty'

Scenario: AAD user not deleted for a nonexistent user 
	Given I have a delete user request for a nonexistent user
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: Get user profile by email
	Given I have a get user profile by email request for an existing email
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved

Scenario: User account not retrieved with a nonexistent email
	Given I have a get user profile by email request for a nonexistent email
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the error response message should contain 'email does not exist'

Scenario: User account not retrieved with an invalid email
	Given I have a get user profile by email request for an invalid email
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False
	And the error response message should contain 'email cannot be empty'
