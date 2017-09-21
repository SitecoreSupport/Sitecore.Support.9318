using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.XA.Foundation.Grid;
using Sitecore.XA.Foundation.Grid.Model;
using Sitecore.XA.Foundation.Grid.Parser;
using Sitecore.XA.Feature.CreativeExchange.Pipelines.Import.RenderingProcessing;
using Sitecore.XA.Feature.CreativeExchange;
using Sitecore.XA.Foundation.IoC;

namespace Sitecore.Support.XA.Feature.CreativeExchange.Pipelines.Import.RenderingProcessing
{
    public class GridStylesImporter : ImportRenderingProcessingBase
    {
        public override void Process(ImportRenderingProcessingArgs args)
        {
            if (CreativeExchangeSettings.SkipGridSystemClassesImprort)
            {
                return;
            }

            var classNames = args.Classes.Except(args.IgnoredClasses).Intersect(args.GridDefinitionClasses.Select(v => v.Name));
            IEnumerable<GridClass> assignedGridClasses = args.GridDefinitionClasses.Where(c => classNames.Contains(c.Name));

            UpdateGridParameters(args, assignedGridClasses);
        }

        protected virtual void UpdateGridParameters(ImportRenderingProcessingArgs args, IEnumerable<GridClass> gridClasses)
        {
            List<GridClass> classes = gridClasses.ToList();
            Item item = args.RenderingSourceItem;

            LayoutField layoutField = new LayoutField(item);
            string presentationXml = layoutField.Value;

            LayoutDefinition definition = LayoutDefinition.Parse(presentationXml);
            var renderingDefinition = definition.GetDevice(args.ImportContext.DeviceId.ToString()).GetRenderingByUniqueId(args.RenderingUniqueId.ToString());
            if (renderingDefinition == null)
            {
                return;
            }

            if (!args.Attributes.ContainsKey("data-gridfield"))
            {
                return;
            }

            string gridParamsField = args.Attributes["data-gridfield"].First();
           /* if (renderingDefinition.Parameters == null)
            {
                Log.Info("ItemID = " + item + "| RenderingItemID" + renderingDefinition.ItemID + "| UID = " + args.RenderingUniqueId.ToString() + "Placeholder " + renderingDefinition.Placeholder, this);
            }
            */
            var parameters = WebUtil.ParseUrlParameters(renderingDefinition.Parameters ?? String.Empty);

            if (!classes.Any() && string.IsNullOrWhiteSpace(parameters[gridParamsField]))
            {
                return;
            }

            var device = args.Page.Database.GetItem(args.ImportContext.DeviceId);
            var gridDefinitionItem = Sitecore.XA.Foundation.IoC.ServiceLocator.Current.Resolve<IGridContext>().GetGridDefinitionItem(args.Page, device);
            parameters[gridParamsField] = new GridDefinition(gridDefinitionItem).InstantiateGridFieldParser().ToFieldValue(classes.Select(c => c.Id));

            renderingDefinition.Parameters = new UrlString(parameters).GetUrl();

            using (new EditContext(item))
            {
                layoutField.Value = definition.ToXml();
            }
        }
    }
}