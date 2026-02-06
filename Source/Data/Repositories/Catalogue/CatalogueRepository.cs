using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Catalogue;

/// <summary>
/// Repository for catalogue_items, catalogue_pages, and catalogue_deals tables.
/// </summary>
public class CatalogueRepository : BaseRepository
{
    private static CatalogueRepository? _instance;
    public static CatalogueRepository Instance => _instance ??= new CatalogueRepository();

    private CatalogueRepository() { }

    #region Catalogue Pages
    public int[] GetPageIds()
    {
        return ReadColumnInt("SELECT indexid FROM catalogue_pages ORDER BY indexid", 0);
    }

    public string[] GetPageData(int pageId)
    {
        return ReadRow(
            "SELECT indexname, minrank, displayname, style_layout, img_header, img_side, label_description, label_misc, label_moredetails FROM catalogue_pages WHERE indexid = @id",
            Param("@id", pageId));
    }

    public string[] GetPageNamesForRank(byte rank)
    {
        return ReadColumn(
            "SELECT indexname FROM catalogue_pages WHERE minrank <= @rank ORDER BY indexid ASC",
            0,
            Param("@rank", rank));
    }

    public int GetPageIdByIndexName(string indexName, byte minRank)
    {
        return ReadScalarInt(
            "SELECT indexid FROM catalogue_pages WHERE indexname = @name AND minrank <= @rank",
            Param("@name", indexName),
            Param("@rank", minRank));
    }
    #endregion

    #region Catalogue Items
    public int[] GetItemTemplateIds(int pageId)
    {
        return ReadColumnInt(
            "SELECT tid FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemTypeIds(int pageId)
    {
        return ReadColumnInt(
            "SELECT typeid FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemLengths(int pageId)
    {
        return ReadColumnInt(
            "SELECT length FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemWidths(int pageId)
    {
        return ReadColumnInt(
            "SELECT width FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemCosts(int pageId)
    {
        return ReadColumnInt(
            "SELECT catalogue_cost FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemDoorFlags(int pageId)
    {
        return ReadColumnInt(
            "SELECT door FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemTradeableFlags(int pageId)
    {
        return ReadColumnInt(
            "SELECT tradeable FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public int[] GetItemRecycleableFlags(int pageId)
    {
        return ReadColumnInt(
            "SELECT recycleable FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string[] GetItemNames(int pageId)
    {
        return ReadColumn(
            "SELECT catalogue_name FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string[] GetItemDescriptions(int pageId)
    {
        return ReadColumn(
            "SELECT catalogue_description FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string[] GetItemCcts(int pageId)
    {
        return ReadColumn(
            "SELECT name_cct FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string[] GetItemColours(int pageId)
    {
        return ReadColumn(
            "SELECT colour FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string[] GetItemTopHeights(int pageId)
    {
        return ReadColumn(
            "SELECT top FROM catalogue_items WHERE catalogue_id_page = @page ORDER BY catalogue_id_index ASC",
            0,
            Param("@page", pageId));
    }

    public string? GetItemCctByTemplateId(int templateId)
    {
        return ReadScalar(
            "SELECT name_cct FROM catalogue_items WHERE tid = @tid",
            Param("@tid", templateId));
    }

    public string? GetItemColourByTemplateId(int templateId)
    {
        return ReadScalar(
            "SELECT colour FROM catalogue_items WHERE tid = @tid",
            Param("@tid", templateId));
    }

    public int GetItemCost(int templateId)
    {
        return ReadScalarInt(
            "SELECT catalogue_cost FROM catalogue_items WHERE tid = @tid",
            Param("@tid", templateId));
    }

    public int GetTemplateIdByCct(string cctName)
    {
        return ReadScalarInt(
            "SELECT tid FROM catalogue_items WHERE name_cct = @name",
            Param("@name", cctName));
    }

    public int GetItemCostByPageAndTemplate(int pageId, int templateId)
    {
        return ReadScalarInt(
            "SELECT catalogue_cost FROM catalogue_items WHERE catalogue_id_page = @page AND tid = @tid",
            Param("@page", pageId),
            Param("@tid", templateId));
    }
    #endregion

    #region Catalogue Deals
    public int[] GetDealItemIds(int dealId)
    {
        return ReadColumnInt(
            "SELECT tid FROM catalogue_deals WHERE id = @id",
            0,
            Param("@id", dealId));
    }

    public int[] GetDealItemAmounts(int dealId)
    {
        return ReadColumnInt(
            "SELECT amount FROM catalogue_deals WHERE id = @id",
            0,
            Param("@id", dealId));
    }
    #endregion
}
