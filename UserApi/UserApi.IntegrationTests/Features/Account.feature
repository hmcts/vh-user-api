@VIH-3606
Feature: Account
	In order to manage to ad groups
	As an api service
	I want to be able to retrieve or return ad groups

Scenario: Get AD group by name
	Given I have a get ad group by name request with the group name 'SSPR Enabled'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: AD group not retrieved with a nonexistent name
	Given I have a get ad group by name request with the group name 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD group not retrieved with an invalid name
	Given I have a get ad group by name request with the group name ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: Get AD group by id
	Given I have a get ad group by id request with the group id '8881ea85-e0c0-4a0b-aa9c-979b9f0c05cd'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: AD group not retrieved with a nonexistent id
	Given I have a get ad group by id request with the group id 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD group not retrieved with an invalid id
	Given I have a get ad group by id request with the group id ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: Get AD groups for a user
	Given I have a get ad groups for a user request for the user id '84fa0832-cd70-4788-8f48-e869571e0c56'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And a list of ad groups should be retrieved

Scenario: AD groups not retrieved for a nonexistent user
	Given I have a get ad groups for a user request for the user id 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD groups not retrieved for an invalid user
	Given I have a get ad groups for a user request for the user id ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: Add a user to a group
	Given I have an add a user to a group request for the user id '84fa0832-cd70-4788-8f48-e869571e0c56' and group 'SSPR Enabled'
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And user should be added to the group

Scenario: User not added to a group with a nonexistent user
	Given I have an add a user to a group request for the user id 'Does not exist' and group 'SSPR Enabled'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False
	And the response message should read 'group already exists'

Scenario: User not added to a group with a nonexistent group
	Given I have an add a user to a group request for the user id '84fa0832-cd70-4788-8f48-e869571e0c56' and group 'Does not exist'
	When I send the request to the endpoint
	Then the response should have the status NotFound and success status False

Scenario: AD groups not added for an invalid user
	Given I have an add a user to a group request for the user id '' and group 'SSPR Enabled'
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False

Scenario: AD groups not added for an invalid group
	Given I have an add a user to a group request for the user id '84fa0832-cd70-4788-8f48-e869571e0c56' and group ''
	When I send the request to the endpoint
	Then the response should have the status BadRequest and success status False