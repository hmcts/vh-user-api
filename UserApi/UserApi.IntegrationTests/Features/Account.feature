@VIH-3606
Feature: Account
	In order to manage to ad groups
	As an api service
	I want to be able to retrieve or return ad groups

Scenario: Get AD group by name
	Given I have a get ad group by name request with a valid group name
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: AD group not retrieved with a nonexistent name
	Given I have a get ad group by name request with a nonexistent group name
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD group not retrieved with an invalid name
	Given I have a get ad group by name request with an invalid group name
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: Get AD group by id
	Given I have a get ad group by id request with a valid group id
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: AD group not retrieved with a nonexistent id
	Given I have a get ad group by id request with a nonexistent group id
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD group not retrieved with an invalid id
	Given I have a get ad group by id request with an invalid group id
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: Get AD groups for a user
	Given I have a get ad groups for a user request for a valid user id
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And a list of ad groups should be retrieved

Scenario: AD groups not retrieved for a nonexistent user
	Given I have a get ad groups for a user request for a nonexistent user id
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD groups not retrieved for an invalid user
	Given I have a get ad groups for a user request for an invalid user id
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

@AddUserToGroup
Scenario: Add a user to a group
	Given I have an add a user to a group request for a valid user id and valid group
	When I send the request to the endpoint
	Then the response should have the status Accepted and success status True
	And user should be added to the group

Scenario: User not added to a group with a nonexistent user
	Given I have an add a user to a group request for a nonexistent user id and valid group
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD groups not added for an invalid user
	Given I have an add a user to a group request for an invalid user id and valid group
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: User not added to a group that they are already a member of
	Given I have an add a user to a group request for an existing user id and existing group
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the response message should read 'group already exists'

Scenario: User not added to a group with a nonexistent group
	Given I have an add a user to a group request for an existing user id and nonexistent group
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD groups not added for an invalid group
	Given I have an add a user to a group request for an existing user id and invalid group
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False