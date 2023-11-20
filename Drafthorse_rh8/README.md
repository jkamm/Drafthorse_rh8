# ![DrafthorseLogo](https://github.com/jkamm/DraftHorse_gh/assets/9583495/06ac40b9-99bc-4328-9671-e6da55de96ec) DraftHorse 

Grasshopper plugin for Rhino 8 (win), helping automate Layout creation and management. 

![DH_ComponentSet](https://github.com/jkamm/DraftHorse_gh/assets/9583495/7946460c-24f0-4e05-aa82-76cbafe302c8)

![DH_ComponentsOnCanvas](https://github.com/jkamm/DraftHorse_gh/assets/9583495/b33f200d-0709-493f-8a3f-38773b4097d2)

v0.0.1-beta
- initial release, based on Drafthorse_gh 0.4.0
- includes versions of Drafthorse_gh components, usually as OBSOLETE
- some components have been updated to use Param_ModelPageView and Param_ModelView (new GH types in Rhino 8)
  
WIP/Goals:

- [ ] Update Example files for all components to demonstrate basic workflows		
- [ ] Check that DisplayMode inputs work in other languages
- [ ] Bake to Layouts Component(to allow programmatic baking of geometry to paperspace with a layout as additional object attribute)
- [ ] Change all object references to DetailView and Layout/PageView params in RH8
- [ ] Add PaperName & Orientation as inputs to New Layout Component
- [ ] Add component to label details (name, auto-number, scale)
- [ ] Add ChangeSpace capability
- [ ] Add capability to hide/show layers in details
- [ ] Add capability to hide/show objects in details
- [ ] Add Rhino PDF default Papernames to Modify Layout component
- [ ] Add units change to Modify Layout component
- [ ] Improve component icons
- [ ] Refactor for OSX compatibility
- [ ] Switch complex components to Variable Input Components to simplify