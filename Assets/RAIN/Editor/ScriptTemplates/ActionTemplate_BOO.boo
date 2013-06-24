import RAIN.Action
import RAIN.Core

class ActionTemplate_BOO(Action): 
	def constructor():
		actionName = "ActionTemplate_BOO"

	def Start(agent as Agent, deltaTime as decimal):
		return ActionResult.SUCCESS
	
	def Execute(agent as Agent, deltaTime as decimal):
		return ActionResult.SUCCESS

	def Stop(agent as Agent, deltaTime as decimal):
		return ActionResult.SUCCESS