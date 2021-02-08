import json
import dash
import dash_core_components as dcc
import dash_html_components as html
from flask import jsonify
import dash_cytoscape as cyto
from dash.dependencies import Input, Output
app=dash.Dash()
server = app.server
with open('siec.json') as f:
    elements = json.load(f)
with open('stylesheets.json') as f:
    stylesheets = json.load(f)
established_routes = []
with open('styles.json') as f:
    styles = json.load(f)
def app_layout(element,styling,stylesheete):
    obj = html.Div([
        html.Center("Nasza sieć TSST:", style={'color': 'black', 'fontSize': 60, 'strong': True}),
        cyto.Cytoscape(
            id='cytoscape',
            elements=element,
            layout={'name': 'preset'
            },
            style=styling['cytoscape'],
            stylesheet=stylesheete,
            responsive = True
        )
    ])
    return obj
app.layout = app_layout(elements,styles,stylesheets)
@server.route('/api/add/<route>')
def route1(route):
    tablica = []
    tablica=route.split('$')
    for i in elements:
        if i["data"]["id"].find("h") == -1:
            pass
        else:
            if i["data"]["ip"]==tablica[0]:
                color_to_append=i["data"]["classes"]
                break
    established_routes.append(route+"-"+color_to_append)
    for i in range(len(tablica)):
        if tablica[i].find("r") == -1:
            pass
        else:
            if(i<(len(tablica)-1)):
                stylesheets.append({"selector": "edge[source = \'"+tablica[i]+"\'][target = \'"+tablica[i+1]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
                stylesheets.append({"selector": "edge[source = \'"+tablica[i+1]+"\'][target = \'"+tablica[i]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
    app.layout = app_layout(elements,styles,stylesheets)
    return jsonify({'message':"Added route"+route})
@server.route('/api/del/<route>')
def route2(route):
    tablica = []
    tablica = route.split("$")
    for i in established_routes:
        stored=i.split("-")
        storedRoute=stored[0].split("$")
        if (tablica[0] == storedRoute[0] and tablica[len(tablica)-1] == storedRoute[len(storedRoute)-1]) or (tablica[len(tablica)-1] == storedRoute[0] and tablica[0] == storedRoute[len(storedRoute)-1]):
            color_to_append = stored[1]
    for i in range(len(tablica)):
        if tablica[i].find("r") == -1:
            pass
        else:
            if(i<(len(tablica)-1)):
                stylesheets.remove({"selector": "edge[source = \'"+tablica[i]+"\'][target = \'"+tablica[i+1]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
                stylesheets.remove({"selector": "edge[source = \'"+tablica[i+1]+"\'][target = \'"+tablica[i]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
    app.layout = app_layout(elements,styles,stylesheets)
    return jsonify({'message':"Deleted route:"+route})
@server.route('/api/breakdown/<route>')
def route3(route):
    tablica = []
    tablica = route.split("$")
    if(len(tablica)==1):
        if(tablica[0][0]=="r"):
            stylesheets.append({"selector": "node[id = \'"+route+"\']","style": {"background-color":"black"}})
        else:
            stylesheets.append({"selector": "node[ip = \'"+route+"\']","style": {"background-color":"black"}})
        stylesheets.append({"selector": "edge[source = \'"+route+"\']","style": {"line-color": "black"}})
        stylesheets.append({"selector": "edge[target = \'"+route+"\']","style": {"line-color": "black"}})
        message = {'message':"Hide node:"+route}
    else:
        stylesheets.append({"selector": "edge[source = \'"+tablica[0]+"\'][target = \'"+tablica[1]+"\']","style": {"line-color": "black"}})
        stylesheets.append({"selector": "edge[source = \'"+tablica[1]+"\'][target = \'"+tablica[0]+"\']","style": {"line-color": "black"}})
        message = {'message':"Hide edge:"+route}
    app.layout = app_layout(elements,styles,stylesheets)
    return jsonify(message)
@server.route('/api/repair/<route>')
def route4(route):
    tablica = []
    tablica = route.split("$")
    if(len(tablica)==1):
        if(tablica[0][0]=="r"):
            stylesheets.remove({"selector": "node[id = \'"+route+"\']","style": {"background-color":"black"}})
        else:
            stylesheets.remove({"selector": "node[ip = \'"+route+"\']","style": {"background-color":"black"}})
        stylesheets.remove({"selector": "edge[source = \'"+route+"\']","style": {"line-color": "black"}})
        stylesheets.remove({"selector": "edge[target = \'"+route+"\']","style": {"line-color": "black"}})
        message = {'message':"Unhide node:"+route}
    else:
        stylesheets.remove({"selector": "edge[source = \'"+tablica[0]+"\'][target = \'"+tablica[1]+"\']","style": {"line-color": "black"}})
        stylesheets.remove({"selector": "edge[source = \'"+tablica[1]+"\'][target = \'"+tablica[0]+"\']","style": {"line-color": "black"}})
        message = {'message':"Unhide edge:"+route}
    app.layout = app_layout(elements,styles,stylesheets)
    return jsonify(message)
@server.route('/api/change/<route>')
def route5(route):
    tablica = []
    changed = False
    tablica = route.split("=")
    routeToChange=tablica[0]
    newRoute=tablica[1]
    print(stylesheets)
    for i in established_routes:
        stored=i.split("-")
        storedRoute=stored[0]
        if routeToChange in storedRoute:
            color_to_append = stored[1]
            established_routes[established_routes.index(i)]=storedRoute[0:storedRoute.index(routeToChange)]+newRoute+i[storedRoute.index(routeToChange)+len(routeToChange):len(i)]
            routeToChangeList = routeToChange.split("$")
            changed = True
            for i in range(len(routeToChangeList)):
                if routeToChangeList[i].find("r") == -1:
                    pass
                else:
                    if(i<(len(routeToChangeList)-1)):
                        stylesheets.remove({"selector": "edge[source = \'"+routeToChangeList[i]+"\'][target = \'"+routeToChangeList[i+1]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
                        stylesheets.remove({"selector": "edge[source = \'"+routeToChangeList[i+1]+"\'][target = \'"+routeToChangeList[i]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
            if changed:
                newRouteList = newRoute.split("$")
                for i in range(len(newRouteList)):
                    if newRouteList[i].find("r") == -1:
                        pass
                    else:
                        if(i<(len(newRouteList)-1)):
                            stylesheets.append({"selector": "edge[source = \'"+newRouteList[i]+"\'][target = \'"+newRouteList[i+1]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
                            stylesheets.append({"selector": "edge[source = \'"+newRouteList[i+1]+"\'][target = \'"+newRouteList[i]+"\'][col = \'"+color_to_append+"\']","style": {"line-color": color_to_append}})
    message = {'message':"Changed route:"+route}
    app.layout = app_layout(elements,styles,stylesheets)
    return jsonify(message)
if __name__ == '__main__':
    app.run_server(debug=False)
