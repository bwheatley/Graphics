using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.VFX;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXRecompileEvent : EventBase<VFXRecompileEvent>, IPropagatableEvent
    {
        public VFXViewController controller;
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
            controller = null;
        }
    }

    partial class VFXViewController : Controller<VisualEffectAsset>
    {
        public void RecompileExpressionGraphIfNeeded()
        {
            if (!ExpressionGraphDirty)
                return;

            ExpressionGraphDirty = false;

            try
            {
                CreateExpressionContext(true /*cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kConnectionChanged*/);
                m_ExpressionContext.Recompile();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            using (VFXRecompileEvent e = VFXRecompileEvent.GetPooled())
            {
                SendEvent(e);
            }
        }

        public void InvalidateExpressionGraph(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (cause != VFXModel.InvalidationCause.kStructureChanged &&
                cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kParamChanged)
                /*use != VFXModel.InvalidationCause.kConnectionChanged &&
                cause != VFXModel.InvalidationCause.kParamChanged &&
                cause != VFXModel.InvalidationCause.kSettingChanged)*/
                return;

            ExpressionGraphDirty = true;
        }

        private void CreateExpressionContext(bool forceRecreation)
        {
            if (!forceRecreation && m_ExpressionContext != null)
                return;

            m_ExpressionContext = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var currentObjects = new HashSet<ScriptableObject>();
            graph.CollectDependencies(currentObjects);

            int nbExpr = 0;
            foreach (var o in currentObjects)
            {
                if (o is VFXSlot)
                {
                    var exp = ((VFXSlot)o).GetExpression();
                    if (exp != null)
                    {
                        m_ExpressionContext.RegisterExpression(exp);
                        ++nbExpr;
                    }
                }
            }
        }

        public bool CanGetEvaluatedContent(VFXSlot slot)
        {
            if (m_ExpressionContext == null)
                return false;
            if (slot.GetExpression() == null)
                return false;
            Profiler.BeginSample("CanGetEvaluatedContent");
            var reduced = m_ExpressionContext.GetReduced(slot.GetExpression());
            var result = reduced != null && reduced.Is(VFXExpression.Flags.Value);
            Profiler.EndSample();
            return result;
        }

        public object GetEvaluatedContent(VFXSlot slot)
        {
            if (!CanGetEvaluatedContent(slot))
                return null;
            var reduced = m_ExpressionContext.GetReduced(slot.GetExpression());
            var result = reduced.GetContent();
            return result;
        }

        private VFXExpression.Context m_ExpressionContext;
        [NonSerialized]
        private bool ExpressionGraphDirty = true;
    }
}
