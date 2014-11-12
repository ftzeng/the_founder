/*
 * The company is the primary entity of the game.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class Company : HasStats {
    public Stat cash;
    public Consultancy consultancy;

    public Company(string name_) {
        name = name_;
    }

    // At what phase the company is operating.
    public enum Phase {
        Local,
        Global,
        Planetary,
        Galactic
    }
    public Phase phase;


    public virtual void Awake() {
        // Default values.
        phase = Phase.Local;
        cash = new Stat("Cash", 100000);
        sizeLimit = 10;
        founders = new List<Worker>();
        _workers = new List<Worker>();
        baseFeaturePoints = 4;
        productPoints = 10;
        lastMonthCosts = 0;
        lastMonthRevenue = 0;
        _items = new List<Item>();
    }


    // ===============================================
    // Worker Management =============================
    // ===============================================

    public int sizeLimit;
    public List<Worker> founders;
    private List<Worker> _workers;
    public ReadOnlyCollection<Worker> workers {
        get { return _workers.AsReadOnly(); }
    }
    public int remainingSpace {
        get { return sizeLimit - _workers.Count; }
    }

    public bool HireWorker(Worker worker) {
        if (_workers.Count < sizeLimit) {
            foreach (Item item in _items) {
                worker.ApplyItem(item);
            }

            _workers.Add(worker);
            return true;
        }
        return false;
    }
    public void FireWorker(Worker worker) {
        foreach (Item item in _items) {
            worker.RemoveItem(item);
        }

        _workers.Remove(worker);
    }

    // Total feature points available.
    private int baseFeaturePoints;
    public FeaturePoints featurePoints {
        get {
            float charisma_   = 0;
            float cleverness_ = 0;
            float creativity_ = 0;
            foreach (Worker w in _workers) {
                charisma_   += w.charisma.value;
                cleverness_ += w.cleverness.value;
                creativity_ += w.creativity.value;
            }

            // You get one feature point for every 10 worker points.
            int charisma   = (int)(charisma_/10)   + baseFeaturePoints;
            int cleverness = (int)(cleverness_/10) + baseFeaturePoints;
            int creativity = (int)(creativity_/10) + baseFeaturePoints;

            // TO DO: bonuses which increase any of these.

            return new FeaturePoints(charisma, cleverness, creativity);
        }
    }


    // ===============================================
    // Product Management ============================
    // ===============================================

    // Total product point capacity.
    public int productPoints;
    public int usedProductPoints {
        get { return products.Sum(p => p.state != Product.State.RETIRED ? p.points : 0); }
    }
    public int availableProductPoints {
        get { return productPoints - usedProductPoints; }
    }

    public List<Product> products = new List<Product>();
    public List<Product> activeProducts {
        get {
            return products.FindAll(p => p.state == Product.State.LAUNCHED);
        }
    }
    public List<Product> developingProducts {
        get {
            return products.FindAll(p => p.state == Product.State.DEVELOPMENT);
        }
    }

    public void StartNewProduct(ProductType pt, Industry i, Market m) {
        Product product = ScriptableObject.CreateInstance<Product>();
        product.Init(pt, i, m);

        // Apply any applicable items to the new product.
        // TO DO: should this be held off until after the product is completed?
        foreach (Item item in _items) {
            foreach (ProductEffect pe in item.effects.products) {
                // If the product effect is indiscriminate (i.e. doesn't specify any aspects), it applies to every product.
                // Otherwise, a product must fit at least one of the aspects to have the effect applied.
                if ((pe.productTypes.Count == 0 && pe.industries.Count == 0 && pe.markets.Count == 0) ||
                    (pe.productTypes.Contains(pt) || pe.industries.Contains(i) || pe.markets.Contains(m))) {
                    product.ApplyBuff(pe.buff);
                }
            }
        }

        products.Add(product);
    }

    public void DevelopProducts() {
        List<Product> inDevelopment = products.FindAll(p => p.state == Product.State.DEVELOPMENT);
        foreach (Product product in inDevelopment) {
            DevelopProduct(product);
        }
    }

    public void HarvestProducts(float elapsedTime) {
        List<Product> launched = products.FindAll(p => p.state == Product.State.LAUNCHED);

        float newRevenue = 0;
        foreach (Product product in launched) {
            newRevenue += product.Revenue(elapsedTime);
        }
        cash.baseValue += newRevenue;
        lastMonthRevenue += newRevenue;
    }

    public void DevelopProduct(Product product) {
        float charisma = 0;
        float creativity = 0;
        float cleverness = 0;
        float progress = 0;

        foreach (Worker worker in workers) {
            // A bit of randomness to make things more interesting.
            charisma += (worker.charisma.value/2) * Random.Range(0.90f, 1.05f);
            creativity += (worker.creativity.value/2) * Random.Range(0.90f, 1.05f);
            cleverness += (worker.cleverness.value/2) * Random.Range(0.90f, 1.05f);
            progress += (worker.productivity.value/2) * Random.Range(0.90f, 1.05f);
        }

        product.Develop(progress, charisma, creativity, cleverness);
    }

    public void ShutdownProduct(Product product) {
        foreach (Item item in _items) {
            foreach (ProductEffect pe in item.effects.products) {
                // If the product effect is indiscriminate (i.e. doesn't specify any aspects), it applies to every product.
                // Otherwise, a product must fit at least one of the aspects to have the effect applied.
                if ((pe.productTypes.Count == 0 && pe.industries.Count == 0 && pe.markets.Count == 0) ||
                    (pe.productTypes.Contains(product.productType) || pe.industries.Contains(product.industry) || pe.markets.Contains(product.market))) {
                    product.RemoveBuff(pe.buff);
                }
            }
        }
        product.Shutdown();
    }

    public void ApplyProductEffect(ProductEffect effect) {
        List<Product> matchingProducts = FindMatchingProducts(effect.productTypes, effect.industries, effect.markets);
        foreach (Product product in matchingProducts) {
            product.ApplyBuff(effect.buff);
        }
    }
    public void RemoveProductEffect(ProductEffect effect) {
        List<Product> matchingProducts = FindMatchingProducts(effect.productTypes, effect.industries, effect.markets);
        foreach (Product product in matchingProducts) {
            product.RemoveBuff(effect.buff);
        }
    }


    // Given an item, find the list of currently active products that
    // match the item's industries, product types, or markets.
    private List<Product> FindMatchingProducts(List<ProductType> productTypes, List<Industry> industries, List<Market> markets) {
        // Items which have no product specifications apply to all products.
        if (industries.Count == 0 && productTypes.Count == 0 && markets.Count == 0) {
            return products;

        } else {
            return products.FindAll(p =>
                industries.Exists(i => i == p.industry)
                || productTypes.Exists(pType => pType == p.productType)
                || markets.Exists(m => m == p.market));
        }
    }



    // ===============================================
    // Financial Management ==========================
    // ===============================================

    // Keep track of each month's costs.
    public float lastMonthCosts;
    public float lastMonthRevenue;
    public void PayMonthly() {
        float toPay = 0;
        foreach (Worker worker in workers) {
            toPay += worker.salary;
        }
        if (consultancy != null) {
            toPay += consultancy.cost;
        }
        cash.baseValue -= toPay;

        // Reset month's costs with this month's costs.
        lastMonthCosts = toPay;

        // Also reset month's revenues.
        lastMonthRevenue = 0;
    }

    public bool Pay(float cost) {
        if (cash.baseValue - cost >= 0) {
            cash.baseValue -= cost;
            lastMonthCosts += cost;
            return true;
        }
        return false;
    }



    // ===============================================
    // Item Management ===============================
    // ===============================================

    public List<Item> _items;
    public ReadOnlyCollection<Item> items {
        get { return _items.AsReadOnly(); }
    }

    public bool BuyItem(Item item) {
        if (Pay(item.cost)) {
            _items.Add(item);

            foreach (ProductEffect pe in item.effects.products) {
                ApplyProductEffect(pe);
            }

            foreach (Worker worker in _workers) {
                worker.ApplyItem(item);
            }

            return true;
        }
        return false;
    }

    public void RemoveItem(Item item) {
        _items.Remove(item);

        foreach (ProductEffect pe in item.effects.products) {
            RemoveProductEffect(pe);
        }

        foreach (Worker worker in _workers) {
            worker.RemoveItem(item);
        }
    }



    // ===============================================
    // Utility =======================================
    // ===============================================

    public override Stat StatByName(string name) {
        switch (name) {
            case "Cash":
                return cash;
            default:
                return null;
        }
    }
}


