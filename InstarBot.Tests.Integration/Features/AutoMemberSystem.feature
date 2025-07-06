@systems
Feature: Auto Member System
	
	The Auto Member System evaluates all new members on the server
	and determines their eligibility for membership through a series
	of checks.
	
	Background: 
		Given the roles as follows:
			| Role ID            | Role Name  |
			| 796052052433698817 | New Member |
			| 793611808372031499 | Member     |
			| 796085775199502357 | Transfemme |
			| 796148869855576064 | 21+        |
			| 796578609535647765 | She/Her    |
			| 966434762032054282 | AMH        |

	Rule: Eligible users should be granted membership.
		Scenario: A user eligible for membership should be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should be granted membership
			
	Rule: Eligible users should not be granted membership if their membership is withheld.
		Scenario: A user eligible for membership should not be granted membership if their membership is withheld.
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+, She/Her and AMH
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should not be granted membership
	
	Rule: New or inactive users should not be granted membership.
		Scenario: A user who joined the server less than the minimum time should not be granted membership
			Given a user that has:
			* Joined 12 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should not be granted membership
			
		Scenario: An inactive user should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 10 messages in the past day
			* Not been punished
			When the Auto Member System processes
			Then the user should not be granted membership
	
	Rule: Auto Member System should not affect Members.
		Scenario: A user who is already a member should not be affected by the auto member system
			Given a user that has:
			* Joined 36 hours ago
			* The roles Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should remain unchanged
		
	Rule: Users should have all minimum requirements for membership
		Scenario: A user that did not post an introduction should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Did not post an introduction
			* Posted 100 messages in the past day
			* Not been punished
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should not be granted membership
			
		Scenario: A user without an age role should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			When the Auto Member System processes
			Then the user should not be granted membership
			
		Scenario: A user without a gender role should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			When the Auto Member System processes
			Then the user should not be granted membership
			
		Scenario: A user without a pronoun role should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, and 21+
			* Posted an introduction
			* Posted 100 messages in the past day
			* Not been punished
			When the Auto Member System processes
			Then the user should not be granted membership
		
	Rule: Gaius should be checked for warnings and caselogs
		Scenario: A user with a warning should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Been issued a warning
			* Joined the server for the first time
			* Not been granted membership before
			When the Auto Member System processes
			Then the user should not be granted membership
			
		Scenario: A user with a caselog should not be granted membership
			Given a user that has:
			* Joined 36 hours ago
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 100 messages in the past day
			* Been issued a mute
			When the Auto Member System processes
			Then the user should not be granted membership
			
	Rule: Users who have been granted membership and left should be granted membership upon rejoining
			
		Scenario: A user who has had membership before should be automatically granted membership
			Given a user that has:
			* Joined 1 hours ago
			* Been granted membership before
			* First joined 240 hours ago 
			* The roles New Member, Transfemme, 21+ and She/Her
			* Posted an introduction
			* Posted 2 messages in the past day
			* Not been punished
			When the user joins the server
			Then the user should be granted membership