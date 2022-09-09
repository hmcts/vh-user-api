Feature: User
	In order to manage to ad users
	As an api service
	I want to be able to retrieve or create ad users

@ignore
Scenario: Create a new hearings reforms user account
	Given I have a new hearings reforms user account request with a valid email
	When I send the request to the endpoint
	Then the response should have the status Created and success status True
	And the user should be added

@ignore
Scenario: User account created for an user with the same name as existing user
	Given I have a new hearings reforms user account request with an existing email
	When I send the request to the endpoint
	Then the response should have the status Conflict and success status False

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

Scenario Outline: Get user by AD user Id
	Given I have a get user by AD user Id request for an existing user with <role> 
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved
	Examples: 
	| role					|
	| Individual			|
	| Representative		|
	| VhOfficer				|
	| CaseAdmin				|
	| Judge					|

Scenario: User account not retrieved for a nonexistent user
	Given I have a get user by AD user Id request for a nonexistent user with Individual
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the error response message should contain 'user does not exist'
	
Scenario: Get user by user principal name
	Given I have a get user by user principal name request for an existing user principal name
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the user details should be retrieved

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

Scenario: Delete an AAD user
	Given I have a new user
	And I have a delete user request for the new user
	When I send the delete request to the endpoint with polling
	Then the response should have the status NoContent and success status True

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
