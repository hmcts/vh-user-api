@VIH-3606
Feature: Account
	In order to manage to ad groups
	As an api service
	I want to be able to retrieve or return ad groups

@AddGroup
Scenario: Get AD group by name
	Given I have a get ad group by name request with a valid group name
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: Get AD group by id
	Given I have a get ad group by id request with a valid group id
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And the ad groups should be retrieved

Scenario: Get AD groups for a user
	Given I have a get ad groups for a user request for a valid user id
	When I send the request to the endpoint
	Then the response should have the status OK and success status True
	And a list of ad groups should be retrieved

Scenario: Add a user to a group
	Given I have an add a user to a group request for a valid user id and valid group
	When I send the request to the endpoint
	Then the response should have the status Accepted and success status True
	And user should be added to the group