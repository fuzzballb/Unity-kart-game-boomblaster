import RAIN.Action
import RAIN.Core

[RAINAction]
class ActionTemplate_BOO(RAINAction): 
	def constructor():
		actionName = "ActionTemplate_BOO"

	def Start(ai as AI, deltaTime as decimal):
		super.Start(ai, deltaTime)
		return
	
	def Execute(ai as AI, deltaTime as decimal):
		return ActionResult.SUCCESS

	def Stop(ai as AI, deltaTime as decimal):
		super.Stop(ai, deltaTime)
		return