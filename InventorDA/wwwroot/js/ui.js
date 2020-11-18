///////////////////////////////////////////////////////////////////////
//// Copyright (c) Autodesk, Inc. All rights reserved
//// Written by Sven Dickmans - TSS EMEA
//// Based on code written by Forge Partner Development
////
//// Permission to use, copy, modify, and distribute this software in
//// object code form for any purpose and without fee is hereby granted,
//// provided that the above copyright notice appears in all copies and
//// that both that copyright notice and the limited warranty and
//// restricted rights notice below appear in all supporting
//// documentation.
////
//// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
//// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
//// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
//// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
//// UNINTERRUPTED OR ERROR FREE.
///////////////////////////////////////////////////////////////////////


let industries, products, lines, configurations;
let configParameters, configLabels;
let currentURN, currentDMSID, currentWSID, currentNumber, currentTitle, countConfigurations, dmsIdConfiguration, nameConfiguration;
let daSettings;
let wsIdConfigurations = "194";


$(document).ready(function() {
    
    $(".content").first().show();
    
    getIndustries();
    getProductLines();
    getProducts();
    
    $("#header-logo").click(function() {
        setContent("landing");
        $("#path").html("").hide();
    });

    // Open selected list of tiles
    $(".nav-industries").click(function() {
        setContent("industries");
    }); 
    $(".nav-lines").click(function() {
        $("#lines").find(".tile").show();
        $("#lines").find(".prefix").remove();
        $("#lines").find(".content-path").hide();
        setContent("lines");
        selectedLevel1 = "";
    });
    $(".nav-products").click(function() {
        $("#products").find(".tile").show();
        $("#products").find(".content-title").html("All Products");
        $("#products").find(".content-path").hide();
        $("#product-line").html("");
        setContent("products");
    });
    

    $(".product-heading").click(function() {
        $(this).next().toggle();
        $(this).toggleClass("expanded");
        $(this).toggleClass("collapsed");
        $(this).find(".zmdi").toggle();
    });
    
    
    $("#product-back").click(function() {
        currentURN    = "";
        currentDMSID  = "";
        currentWSID   = "";
        $("#products").show(); 
        $("#product").hide();
    });
    
    
    $(".content-path").click(function() {
        let id = $(this).attr("data-id");
        setContent(id);
    });
    
    
    // Download buttons
    $(".button.download").click(function() {
        
//        let url = $(this).attr("data-url");
//        document.getElementById('frame-download').src = url;
        
        let extension = $(this).attr("data-extension");
        
        console.log(extension);
        
        $.get('api/FLC/GetAttachments/' + wsIdConfigurations + '/' + dmsIdConfiguration, function (data, status) {
                        
            let response  = JSON.parse(data);
            
            console.log(response.attachments);
            
            for(attachment of response.attachments) {
                if(attachment.type.extension === extension) {
                    document.getElementById('frame-download').src = attachment.url;
                }
            }
                    
        });
        
    });
    
   
    // Toggle Product Configuration Dialog
    $("#button-configure").click(function() {
        $("#product-options").show();
        $("#product-details").hide();
        $("#button-configure").hide();
        $("#button-reset").show();
        $("#button-cancel").show();
        $("#button-apply").show();
        $("#button-download-pdf").hide();
        $("#button-download-ipt").hide();
    });
    $("#button-cancel").click(function() {
        $("#product-options").hide();
        $("#product-details").show();
        $("#button-configure").show();
        $("#button-reset").hide();
        $("#button-cancel").hide();
        $("#button-apply").hide();
        $("#button-download-pdf").show();
        $("#button-download-ipt").show();
//        $("#button-reset").click();
    });
    $("#button-reset").click(function() {
        
        $(".toggle-value").removeClass("selected");
        $(".toggle-value.default").addClass("selected");
        
        $("input.slider").each(function() {
            let elemParameter = $(this).closest(".parameter");
            $(this).val(elemParameter.attr("data-default"));
            $(this).parent().next().html(elemParameter.attr("data-default"));
        });
        
        $("#button-apply").removeClass("default").addClass("disabled");
        $("#button-reset").addClass("disabled");
        
    });
    
    
    // Forge Processing
    $("#button-apply").click(function() {
        
        let labels = [];
        let params = { 
            'browserconnectionId' : connectionId,
            'ModelAttributes' : []
        };
        
        $(".toggle-value.selected").each(function() {
            
            let key   = $(this).closest(".parameter").attr("data-parameter");
            let value = $(this).attr("value");
            let label = $(this).closest(".parameter-input").prev().html();
            
            params.ModelAttributes.push({
                'name' : key,
                'value': value
            });
            
            labels.push(label);
            
        });
        
        $("input.slider").each(function() {
            
            let key   = $(this).closest(".parameter").attr("data-parameter");
            let value = $(this).val();
            let label = $(this).closest(".parameter-input").prev().html();
            
            params.ModelAttributes.push({
                'name' : key,
                'value': value
            });
            
            labels.push(label);
            
        });
        
        
        configParameters    = JSON.stringify(params.ModelAttributes);
        configLabels        = labels.toString();
        
        if(newConfiguration()) {
        
            $("#back").show();
            $("#processing").show();
            
            countConfigurations++;
            
//            console.log("countConfigurations = " + countConfigurations);
            
            let configNumber = countConfigurations.toFixed(0);
            
            if(countConfigurations <  1000) configNumber = "0" + configNumber;
            if(countConfigurations <   100) configNumber = "0" + configNumber;
            if(countConfigurations <    10) configNumber = "0" + configNumber;
            
            nameConfiguration = currentNumber + "_" + currentTitle + "_" + configNumber;
            nameConfiguration = nameConfiguration.replace(/ /g, "_");
            
            params.browserconnectionId  = connectionId; 
            params.inputmodel           = daSettings.FILENAME_MODEL; 
            params.inputdrawing         = daSettings.FILENAME_DRAWING; 
            params.configurationmodel   = daSettings.FOLDER_NAME; 
            params.configurationId      = nameConfiguration; 

            $.post({
                url         : 'api/forge/params/designautomation',
                contentType : 'application/json',
                data        : JSON.stringify(params)
            });
            
        }
        
    });
    $("#processing-cancel").click(function() {
        $(".dialog").hide();
        $("#back").hide();
    });
    
    
    // Image slide
    $("#product-slider-prev").click(function() {

        if($(this).hasClass("disabled")) return;
        
        let elemDot = $(".zmdi-dot-circle");
            elemDot.removeClass("zmdi-dot-circle");
            elemDot.addClass("zmdi-circle-o");
        
        let elemPrev = elemDot.prev();
            elemPrev.addClass("zmdi-dot-circle");
            elemPrev.removeClass("zmdi-circle-o");
        
        if(elemPrev.hasClass("dot-viewer")) {

            $("#viewer").show();
            $(".product-image").hide();
        
        } else {
        
            let idImage = elemPrev.attr("data-image");

            $(".product-image").each(function() {
                if($(this).attr("data-image") === idImage) { $(this).show(); } else { $(this).hide(); }
            });
            
        }
        
        if(elemPrev.prev().length === 0) {
            $("#product-slider-prev").addClass("disabled");
            $("#product-images").css("z-index", "-1");
        }
        
        $("#product-slider-next").removeClass("disabled");
        
    
    });
    $("#product-slider-next").click(function() {

        if($(this).hasClass("disabled")) return;
        
        let elemDot = $(".zmdi-dot-circle");
            elemDot.removeClass("zmdi-dot-circle");
            elemDot.addClass("zmdi-circle-o");
        
        let elemNext = elemDot.next();
            elemNext.addClass("zmdi-dot-circle");
            elemNext.removeClass("zmdi-circle-o");
        
        let idImage = elemNext.attr("data-image");
        
        $(".product-image").each(function() {
            if($(this).attr("data-image") === idImage) { $(this).show(); } else { $(this).hide(); }
        });
        
        if(elemNext.next().length === 0) {
            $("#product-slider-next").addClass("disabled");
        }
        
        
        $("#product-slider-prev").removeClass("disabled");
        $("#product-images").css("z-index", "1");
        $("#viewer").hide();
        
    });
    
    
    // Request creation dialog user interactions
    $("#button-request-quote").click(function() {
        $("#consent-off").show();
        $("#consent-on").hide();
        $("#request-consent").removeClass("checked");
        $("#request-send").addClass("disabled");
        $("#back").show();
        $("#request-dialog").show();
        setQuoteRequestDetails();
    });
    $("#request-close").click(function() {
        $("#back").hide();
        $("#request-dialog").hide();
    });  
    $("#request-cancel").click(function() {
        $("#back").hide();
        $("#request-dialog").hide();
    });
    $("#request-send").click(function() {
        if($(this).hasClass("disabled")) return;
        $("#request-dialog").hide();
        $("#request-process").show();
        sendRequest();
    });
    $("#request-consent").click(function() {
        $(this).toggleClass("checked");    
        $(this).find(".zmdi").toggle();
        $("#request-send").toggleClass("disabled");
    });
    $(".request-heading").click(function() {
        $(this).find("i").toggle();
        $(this).next().toggle();
        $(this).toggleClass("expanded");
    });
    $("#request-confirmation-button").click(function() {
        $("#back").hide();
        $("#request-confirmation").hide();
    });
    
    
});


function setContent(id) {
    
    $("#configurator").hide();
    $(".content").hide();
    $("#main").scrollTop();
    $("#" + id).show();
    
}


// Load data into list views
function getIndustries() {
    
    $.get('api/FLC/GetTableau/129/793021', function (data, status) {
    
        let elemList    = $("#industries-list");
        industries      = JSON.parse(data);
        
        removeObsoleteIndustries(elemList);
    
        for(industry of industries.items) {
            
            let mktName     = getItemValue(industry, "MARKETING_NAME_EN");
            let descriptor  = getItemValue(industry, "DESCRIPTOR");
            let isNew       = updateExistingIndustry(elemList, industry, descriptor, mktName);
            
            if(isNew) {
                
                let exists      = false;
                
                let elemTile = appendTile(null, industry, "industry", "half", "");
                    elemTile.attr("data-descriptor", descriptor);
                    elemTile.attr("data-sort", mktName);
                    elemTile.click(function() {
                        $("#lines").find(".content-path").show();
                        setTitle("lines", $(this).find(".tile-title").html());
                        setList("lines", $(this).attr("data-descriptor"), null);
                        selectedLevel1 = descriptor;
                    });

                elemList.children().each(function() {

                    if(!exists) {
                        let listName = $(this).attr("data-sort");
                        if(listName > mktName) {
                            elemTile.insertBefore($(this));
                            exists = true;
                        }
                    }

                });

                if(!exists) elemList.append(elemTile); 
                
            }
            
        }    
    
        setTimeout(function(){ getIndustries(); }, 5000);
        
    });
    
}
function getProductLines() {
    
//    console.log(" getProductLines START");
    
    $.get('api/FLC/GetTableau/132/793023', function (data, status) {
    
        lines = JSON.parse(data);
        
        for(line of lines.items) {
            let elemTile = appendTile($("#lines-list"), line, "line", "standard", "PRODUCT_LINE");
                elemTile.attr("data-descriptor", getItemValue(line, "DESCRIPTOR"));
        }    
        
        

        $(".line").click(function() {
            $("#products").find(".content-path").show();
            let title = $(this).find(".tile-title").html();
            let elemTitle = $("#products").find(".content-title");
                elemTitle.html(title);
            setTitle("products", $("#lines").find(".prefix").html());
            setList("products", $(this).attr("data-descriptor"), $(this).attr("data-parent"));
            $("#product-line").html(title);
        });
        
    });
    
}
function getProducts() {
    
//    $.get('api/FLC/SearchItems/131?query="-"', function (data, status) {
    $.get('api/FLC/GetTableau/131/793018', function (data, status) {
        
        products = JSON.parse(data).items;
        
//        console.log(products);
        
        for(product of products) {
            
//            console.log(product);
            
            let elemTile = appendTile($("#products-list"), product, "product", "max", "PRODUCT_FAMILY", "INDUSTRIES");
                elemTile.attr("data-viewable", getItemValue(product, "VIEWABLE_URN"));
//                elemTile.attr("data-level-1", getItemValue(product, "INDUSTRY"));
//                elemTile.attr("data-level-2", getItemValue(product, "PRODUCT_FAMILY"));
        } 

        $(".product").click(function() {
            $("#products").hide();
            $("#product").show();
            setProduct($(this).attr("data-urn"));
             
        });
         
    });
    
}
function appendTile(elemParent, data, className, size, fieldIDParent, fieldIDRoot) {
    
//    log("appendTile", "START");
    
    
    let imageFilename = getItemValue(data, "IMAGE_FILENAME");
    
    
//    let urn = getItemURN(data);
    
//    log("appendTile", "imageFilename = " + imageFilename);
     
    let elemTile = $("<div></div>");
        elemTile.attr("data-urn", getItemURN(data));
        elemTile.addClass("tile");
        elemTile.addClass(className);
        elemTile.addClass(size);
    
    if(elemParent !== null) elemTile.appendTo(elemParent);
        
    let elemTileGraphic = $("<div></div>");
        elemTileGraphic.addClass("tile-graphic");
//        elemTileGraphic.addClass("tile-image");
//        elemTileGraphic.css("background", "../images/" + imgBase + "/" + imageFilename);
//        elemTileGraphic.css("background-image", "url(../images/" + imgBase + "/" + imageFilename + ")");
//        elemTileGraphic.html("../images/" + imgBase + "/" + imageFilename);
//        elemTileGraphic.html("../images/" + imgBase + "/" + imageFilename);
//        elemTileImage.html("<img src=" + getItemValue(data, "FILENAME"));
        elemTileGraphic.appendTo(elemTile);
    
    let elemTileImage = $("<img>");
        elemTileImage.addClass("tile-image")
        elemTileImage.attr("src", "../images/" + imageFilename);
        elemTileImage.appendTo(elemTileGraphic);

    let elemTileTitle = $("<div></div>");
        elemTileTitle.addClass("tile-title");
        elemTileTitle.html(getItemValue(data, "MARKETING_NAME_EN"));
        elemTileTitle.appendTo(elemTile);

    let elemTileDescription = $("<div></div>");
        elemTileDescription.addClass("tile-detail");
        elemTileDescription.html(getItemValue(data, "MARKETING_TEXT_EN"));
        elemTileDescription.appendTo(elemTile);
    
    if(fieldIDParent !== null) {
        if(fieldIDParent !== "") {
            elemTile.attr("data-parent", getItemValue(data, fieldIDParent));
        }
    }
    
    if(fieldIDRoot !== null) {
        if(fieldIDRoot !== "") {
            elemTile.attr("data-root", getItemValue(data, fieldIDRoot));
        }
    }
    
    return elemTile;
    
}
function removeObsoleteIndustries(elemList) {
    
    elemList.children().each(function() {
    
        let listURN = $(this).attr("data-urn");
        let keep = false;

        for(industry of industries.items) {

            let newURN = industry.item.urn;

            if(listURN === newURN) {
                keep = true;
                continue;
            }

        }

        if(!keep) $(this).remove();

    });
    
}
function updateExistingIndustry(elemList, industry, descriptor, mktName) {
 
    let isNew = true;
    
    elemList.children().each(function() {

        if(isNew) {
        
            let listURN = $(this).attr("data-urn");
            let newURN  = industry.item.urn;

            if(listURN === newURN) {
                
                $(this).attr("data-sort", mktName);
                $(this).attr("data-descriptor", descriptor);
                
                let elemTitle = $(this).find(".tile-title");
                    elemTitle.html(mktName);
                
                isNew = false;
            }

        }

    });
    
    return isNew;
    
}


// Parse response data
function getSectionValue(data, fieldID) {
    
    let result = "";
    
    for(section of data.sections) {
        for(field of section.fields) {

            let temp = field.urn.split(".");
            let urn = temp[temp.length - 1];

            if(urn === fieldID) {
                if(field.value === null) {
                    result = "";
                } else if(typeof field.value === "object") {
                    result = field.value.urn;
                } else {
                    result = field.value;
                }
            }

        }
    }
    
    return result;
    
}
function getItemURN(data) {
    
    if(data.hasOwnProperty("urn")) return data.urn;
    else return data.item.urn;
    
}
function getItemValue(data, fieldID) {
    
//    log("getItemValue", "START");
    
    let result = null
    
    if(data.hasOwnProperty("sections")) {
        
        for(section of data.sections) {
            if(result === null) {
                result = getFieldValue(section, fieldID);
            }
        }
        
    } else result = getFieldValue(data, fieldID);
    
    return result;
    
}
function getFieldValue(data, fieldID) {
    
//    log("getFieldValue", "START");
//    log("getFieldValue", "fieldID = " + fieldID);
//    log("getFieldValue", "data.fields.length = " + data.fields.length);
    
    let result = null;
    
    for(field of data.fields) {
            
        let temp = field.urn.split(".");
        let urn = temp[temp.length - 1];
        
        if(urn === fieldID) {
            if(field.value === null) {
                result = "";
            } else if(typeof field.value === "object") {
                if(Array.isArray(field.value)) {
                    let temp = [];
                    for(value of field.value) temp.push(value.title);
                    result = temp.toString();
                } else {
                    result = field.value.title;
                }
            } else {
                result = field.value;
            }
        }
            
    }
    
    return result;
    
}
function getRowValue(row, fieldID) {
    
    let result = "";
    
    for(field of row.rowData) {
            
        let temp = field.urn.split(".");
        let urn = temp[temp.length - 1];
            
        if(urn === fieldID) {
            if(field.value === null) {
                result = "";
            } else if(typeof field.value === "object") {
                result = field.value.urn;
            } else {
                result = field.value;
            }
        }
            
    }
    
    return result;
    
}
function getDMSID(data) {
 
    let temp = data.__self__.split("/");
    
    return temp[temp.length - 1];
    
}


function setStep(idNext, idPrev, label) {
    
    let elemPath = $("#" + idNext).find(".content-path");
    elemPath.html("");
        
    elemPath.show();
    
    let elemStep = $("<div></div>");
        elemStep.html(label);
        elemStep.addClass("step");   
        elemStep.attr("data-id", idPrev);
        elemStep.appendTo(elemPath);
    
    elemStep.click(function() {
        let path = $("#path");
        let id = $(this).attr("data-id");
        setContent(idPrev);
        $(this).nextAll().remove();
        $(this).remove(); 
        if(path.children().length === 0) path.hide();
    });
    
}
function setTitle(id, title) {
    
    let elemPrefix = $("<span></span>");
        elemPrefix.html(title);
        elemPrefix.addClass("prefix");
    
    let elemHeader = $("#" + id).find(".content-title");
        elemHeader.find("span").remove();
        elemHeader.prepend(elemPrefix);
    
}
function setList(id, parent, root) {
    
    let elemList = $("#" + id);
    
    elemList.find(".tile").each(function() {
    
        let show = false;
        
        if($(this).attr("data-parent") === parent) {
            if(root === null) {
                show = true;
            } else if($(this).attr("data-root").indexOf(root) > -1) {
                show = true;
            }
        }
            
        if(show) $(this).show(); else $(this).hide();
            
    });
    
    setContent(id);
    
}


// Once product is clicked in the view, fetch further details
function setProduct(urn) {
    
//    console.log("   setProduct : urn = " + urn);
//    console.log("   setProduct : products.length = " + products.length);
    
    for(product of products) {
        
        let urnProduct = getItemURN(product);
        
        if(urnProduct === urn) {
            
            let temp        = urnProduct.split(".");
            currentURN      = urnProduct;
            currentDMSID    = temp[temp.length - 1];
            currentWSID     = temp[temp.length - 2];
            
            resetProduct();
            setProductDetails(product);
            setProductFiles(product);
            setProductOptions(product);
            setProductConfigurations(product);
            setProductViewer(product);
            
        }
    }
    
}
function resetProduct() {
    
    // Reset Images
    $("#product-images").html("");
    
    
    // Reset Image Slider
    $("#product-slider-prev").addClass("disabled");
    $("#product-slider-list").html("");
    $("#product-slider-next").addClass("disabled");

    // Reset to default view
    //$("#button-cancel").click();
    
    
    // Reset Product Details
    $("#product-specification-list").html("");
    $("#product-files-list").html("");
    $("#product-options").html("");
    $("#product-details").show();
    $("#product-options").hide();
    
    
    // Reset buttons
    $("#button-cancel").hide();
    $("#button-reset").hide().addClass("disabled");
    $("#button-apply").hide();
    $("#button-download-pdf").show();
    $("#button-download-ipt").show();
    $("#button-configure").show();
    $("#button-apply").removeClass("default").addClass("disabled");
    
    
    currentNumber = "";
    currentTitle  = "";
    
    // Reset list of configurations
    dmsIdConfiguration = "";
    configurations = [];
    daSettings = [];
    
}
function setProductDetails(product) {
    
//    log("setProductDetails", "START");
    
//    $("#product-line" ).html(getItemValue(product, "PRODUCT_FAMILY"));
    $("#product-title").html(getItemValue(product, "MARKETING_NAME_EN"));
    currentNumber = getItemValue(product, "NUMBER");
    currentTitle  = getItemValue(product, "TITLE");
    
    $.get('api/FLC/GetItemDetails/' + currentWSID + '/' + currentDMSID, function (data, status) {

        let jsonData = JSON.parse(data);
        
        getDefaultConfiguration(jsonData.sections);
        
        var elem = document.createElement('textarea');
            elem.innerHTML = getItemValue(jsonData, "MARKETING_DESCRIPTION_EN");
        
        $("#product-text" ).html(elem.value);
        
        for(section of jsonData.sections) {

            if(section.title === "Product Specification") {

                for(field of section.fields) {
                    
                    let elemSpec = $("<div></div>");
                        elemSpec.addClass("product-spec");
                        elemSpec.appendTo($("#product-specification-list"));  
                    
                    let elemSpecLabel = $("<div></div>");
                        elemSpecLabel.addClass("product-spec-label");
                        elemSpecLabel.html(field.title);
                        elemSpecLabel.appendTo(elemSpec);        
                    
                    let elemSpecValue = $("<div></div>");
                        elemSpecValue.addClass("product-spec-value");
                        elemSpecValue.html(field.value);
                        elemSpecValue.appendTo(elemSpec);

                }
                
//            } else if(section.title === "Product Description") {
//                
//                for(let i = 0; i < section.fields.length; i++) {
//                
//                    let field       = section.fields[i];
//                    let temp        = field.urn.split(".");
//                    let fieldURN    = temp[temp.length - 1];
//                    
//                    if(fieldURN.indexOf("IMAGE_LINK_") === 0) {
//                        
//                        if(field.value !== "") {
//                            
//                            let elemDot = $("<i></i>");
//                                elemDot.addClass("dot-image");
//                                elemDot.addClass("zmdi");
//                                elemDot.addClass("zmdi-circle-o");
//                                elemDot.attr("data-image", fieldURN);
//                                elemDot.appendTo($("#product-slider-list"));                      
//                            
//                            let elemImage = $("<div></div>");
//                                elemImage.addClass("product-image");
//                                elemImage.attr("data-image", fieldURN);
//                                elemImage.appendTo(elemImages);
//  
//                            let elemImagePicture = $("<div></div>");
//                                elemImagePicture.addClass("product-image-picture");
//                                elemImagePicture.appendTo(elemImage);
//                            
//                            let elemImagePictureFile = $("<img></img>");
//                                elemImagePictureFile.attr("src", "/images" + field.value);
//                                elemImagePictureFile.appendTo(elemImagePicture);
//                            
//                            
//                            let elemImagLabel = $("<div></div>");
//                                elemImagLabel.addClass("product-image-title");    
//                                elemImagLabel.html(section.fields[i+1].value);
//                                elemImagLabel.appendTo(elemImage);
//                            
//                            i++;
//                            
//                            $("#product-slider-next").removeClass("disabled");
//                            
//                        }
//                        
//                    }
//                
//                }   
            
            } else if(section.title === "Product Images") {
                
                for(let i = 0; i < section.fields.length; i++) {
                
                    let field       = section.fields[i];
                    let temp        = field.urn.split(".");
                    let fieldURN    = temp[temp.length - 1];
                    
                    if(fieldURN.indexOf("IMAGE_TITLE") < 0) {
                        if(fieldURN.indexOf("IMAGE_") === 0) {
                            if(field.type.title === "Image") {
                                if(field.value !== null) {

                                    let fieldIDImageTitle = fieldURN.replace(/IMAGE_/g, "IMAGE_TITLE_");
                                    let imageTitle = getItemValue(section, fieldIDImageTitle);

                                    $.get('api/FLC/GetImagebBlob?link=' + field.value.link, function (data, status) {

                                        let elemDot = $("<i></i>");
                                            elemDot.addClass("dot-image");
                                            elemDot.addClass("zmdi");
                                            elemDot.addClass("zmdi-circle-o");
                                            elemDot.attr("data-image", fieldURN);
                                            elemDot.appendTo($("#product-slider-list"));                      

                                        let elemImage = $("<div></div>");
                                            elemImage.addClass("product-image");
                                            elemImage.attr("data-image", fieldURN);
                                            elemImage.appendTo($("#product-images"));

                                        let elemImagePicture = $("<div></div>");
                                            elemImagePicture.addClass("product-image-picture");
                                            elemImagePicture.appendTo(elemImage);

                                        let elemImagePictureFile = $("<img></img>");
                                            elemImagePictureFile.attr("src", 'data:' + data.type + ';base64,' + data.base64String);
                                            elemImagePictureFile.appendTo(elemImagePicture);


                                        let elemImagLabel = $("<div></div>");
                                            elemImagLabel.addClass("product-image-title");    
                                            elemImagLabel.html(getItemValue(section, fieldIDImageTitle));
                                            elemImagLabel.html(imageTitle);
                                            elemImagLabel.appendTo(elemImage);

                                        i++;

                                        $("#product-slider-next").removeClass("disabled");

                                    });

                                }
                            }
                        }
                    }
                }
             
            } else if(section.title === "Design Automation Settings") {
                
                for(let i = 0; i < section.fields.length; i++) {
                
                    let field       = section.fields[i];
                    let temp        = field.urn.split(".");
                    let fieldURN    = temp[temp.length - 1];
                    
                    daSettings[fieldURN] = field.value;
                
                }
                
//                console.log(daSettings);
                
            }
        }
    });
    
}
function setProductFiles(product) {
    
    $.get('api/FLC/GetAttachments/' + currentWSID + '/' + currentDMSID, function (data, status) {

        let response  = JSON.parse(data);
        let elemList  = $("#product-files-list");
        
        if(currentURN === response.item.urn) {
        
            elemList.children().each(function() {

                let listName = $(this).attr("data-name");
                let remove   = true;

                for(attachment of response.attachments) {
                    if(attachment.name === listName) {
                        remove = false;
                    }
                }

                if(remove) $(this).remove();

            });


            for(attachment of response.attachments) {

                let add  = true;
                let name = attachment.name;
                let type = attachment.type.fileType;
                let icon = "type-default";        
                let size = Number(attachment.size);
                    size = Math.round(size / 1024, 1);

                elemList.children().each(function() {

                    let elemListed = $(this);

                    if(name === elemListed.attr("data-name")) {
                        add = false;
                        elemListed.attr("data-url", attachment.url);
                        elemListed.find(".product-file-size").html(size + " kB");
                    }

                });

                if(add) {

                         if(type === "Microsoft Excel"      ) { icon = "type-xlsx"; }
                    else if(type === "Adobe PDF"            ) { icon = "type-pdf";  }
                    else if(type === "Microsoft PowerPoint" ) { icon = "type-pptx"; }

                    let elemFile = $("<div></div>");
                        elemFile.addClass("product-file");
                        elemFile.attr("data-name", name);
                        elemFile.attr("data-url", attachment.url);
                        //elemFile.appendTo(elemList);
                        elemFile.prependTo(elemList);

                    let elemFileIcon = $("<div></div>");
                        elemFileIcon.addClass("product-file-icon");
                        elemFileIcon.addClass(icon);
                        elemFileIcon.appendTo(elemFile);

                    let elemFileTitle = $("<div></div>");
                        elemFileTitle.addClass("product-file-title");
                        elemFileTitle.html(attachment.resourceName);
                        elemFileTitle.appendTo(elemFile);

                    let elemFileSize = $("<div></div>");
                        elemFileSize.addClass("product-file-size");
                        elemFileSize.html(size + " kB");
                        elemFileSize.appendTo(elemFile);

                    elemFile.click(function() {
                        let url = $(this).attr("data-url");
                        document.getElementById('frame-download').src = url;
                    });

                }

            }

            setTimeout(function(){ 
                if(currentURN === response.item.urn) {
                    setProductFiles(product); 
                }
            }, 10000);
                        
        }
        
    });
    
}
function setProductOptions(product) {
    
    let elemList = $("#product-options");

    $.get('api/FLC/GetGridRows/' + currentWSID + '/' + currentDMSID, function (data, status) {

        let rows = JSON.parse(data).rows;
        
        for(row of rows) {
            row.SORT = getRowValue(row, "SORT");
        }
        
        rows.sort(function(a, b){
            var nameA=a.SORT, nameB=b.SORT
            if (nameA < nameB) //sort string ascending
                return -1 
            if (nameA > nameB)
                return 1
            return 0 //default return value (no sorting)
        });
        
        for(row of rows) {
            
            let parameter   = getRowValue(row, "PARAMETER");
            let label       = getRowValue(row, "LABEL");
            let selected    = getRowValue(row, "DEFAULT_VALUE");
            let values      = getRowValue(row, "VALUES");
            let range       = getRowValue(row, "VALUES");
            
            let elemInput = $("<input>");

            
            let elemOption = $("<div></div>");
                elemOption.attr("data-parameter", parameter);
                elemOption.attr("data-label", label);
                elemOption.attr("data-default", selected);
                elemOption.attr("data-value", selected);
                elemOption.attr("class", "parameter");
                elemOption.appendTo(elemList);
            
            let elemOptionLabel = $("<div></div>");
                elemOptionLabel.html(label);
                elemOptionLabel.addClass("parameter-title");
                elemOptionLabel.appendTo(elemOption);
            
            let elemOptionInput = $("<div></div>");
                elemOptionInput.addClass("parameter-input");
                elemOptionInput.appendTo(elemOption);
            
            
            if(values.indexOf(",") > -1) {
                
                elemInput = $("<div></div>");
                elemInput.addClass("toggle");
                
                let options = values.split(",");
                let nameClass = isNumericRange(options) ? "numeric" : "text";
                   
                 for(option of options) {
                    let elemOption = $("<div></div>");
                        elemOption.addClass("toggle-value");
                        elemOption.addClass(nameClass);
                        elemOption.attr("value", option);
                        elemOption.html(option);
                        elemOption.appendTo(elemInput);
                        elemOption.click(function() {
                            $(this).closest(".parameter").attr("data-value", $(this).html());
                            $(this).addClass("selected");
                            $(this).siblings().removeClass("selected");
                            $("#button-reset").removeClass("disabled");
                            $("#button-apply").removeClass("disabled");
                            $("#button-apply").addClass("default");
//                            $("#product-toolbar").hide();
//                            $("#product-toolbar-update").show();
                        });

                        if(option === selected) {
                            elemOption.addClass("selected");
                            elemOption.addClass("default");
                        }

                }
                    
            } else if(values.indexOf("-") > -1) {
                
                let steps  = values.split("/");
                let ranges = steps[0].split("-");
                
                elemInput.attr("type", "range");
                elemInput.attr("min", ranges[0]);
                elemInput.attr("max", ranges[1]);
                elemInput.attr("step", steps[1]);
                elemInput.attr("value", selected);
                elemInput.addClass("slider");
                
                elemInput.on('input change', function() {
                    $(this).closest(".parameter").attr("data-value", $(this).val());
                    $(this).parent().next().html( $(this).val() );
                    $("#button-reset").removeClass("disabled");
                    $("#button-apply").removeClass("disabled");
                    $("#button-apply").addClass("default");
                });
                
                elemOptionInput.addClass("with-value");
                
                let elemOptionValue = $("<div></div>");
                    elemOptionValue.addClass("parameter-value");
                    elemOptionValue.html(selected);
                    elemOptionValue.appendTo(elemOption);
                
            }
            
            elemOptionInput.append(elemInput);
            
            elemInput.attr("name", parameter);
            elemInput.attr("id", parameter);
                
        }
               
    });
    
}
function setProductConfigurations(product) {
    
//    console.log("setProductConfigurations: START");
    
    let descriptor = getItemValue(product, "DESCRIPTOR");
    
    countConfigurations = 0;
    
//    console.log("setProductConfigurations: descriptor = " + descriptor);
    
    $.get('api/FLC/SearchItems/194?query="' + descriptor + '"', function(data, status) {
            
//        console.log("setProductConfigurations: data = " + data);
        
        if(data !== "") {
            
            let configurationsData = JSON.parse(data).items;
            
//            console.log(configurationsData);
            
            countConfigurations = configurationsData.length;
            
//            console.log(countConfigurations);
//            console.log("countConfigurations =" + countConfigurations);
        
            for(configuration of configurationsData) {

                configurations.push({
                    "parameters" : getSectionValue(configuration, "CONFIG_PARAMETERS"),
                    "viewable"   : getSectionValue(configuration, "VIEWABLE_URN"),
                    "pdf"        : getSectionValue(configuration, "URL_PDF"),
                    "ipt"        : getSectionValue(configuration, "URL_IPT"),
                    "idw"        : getSectionValue(configuration, "URL_IDW"),
                    "dmsId"      : getDMSID(configuration)
                });

            }
            
//            console.log(configurations);
            
        }
                                
    });
    
}
function setProductViewer(product) {
    
//    log("setProductViewer", "START");
    
//    console.log(" >> setProductViewer START");
    
    let urn = getItemValue(product, "VIEWABLE_URN");
    
    if(urn === null) return;
    if(urn === ""  ) return;
     
    initViewer(urn, function() {
//        log("setProductViewer", "Viewer init done");
    });
    
    let elemDot = $("<i></i>");
        elemDot.addClass("dot-viewer");
        elemDot.addClass("zmdi");
        elemDot.addClass("zmdi-dot-circle");
        elemDot.prependTo($("#product-slider-list"));  
    
}
function isNumericRange(options) {
    
    for(option of options) {
        if(isNaN(option)) return false;
    }
    
    return true;
    
}
function getDefaultConfiguration(sections) {
    
    for(section of sections) {
        
        for(field of section.fields) {
                    
            let temp        = field.urn.split(".");
            let fieldURN    = temp[temp.length - 1];
                    
            if(fieldURN === "DEFAULT_CONFIGURATION") {
                
                if(field.value !== null) {
                
                    let dataConfiguration = field.value.urn.split(".");
                    dmsIdConfiguration = dataConfiguration[dataConfiguration.length - 1];
                    
//                    $.get('api/FLC/GetAttachments/' + dataConfiguration[dataConfiguration.length - 2] + '/' + dmsIdConfiguration, function (data, status) {
//                        
//                        let response  = JSON.parse(data);
//                        
//                        for(attachment of response.attachments) {
//                            
//                                 if(attachment.type.id === "dwfx") { $("#button-download-ipt").attr("data-url", attachment.url).removeClass("disabled"); } 
//                            else if(attachment.type.id === "pdf" ) { $("#button-download-pdf").attr("data-url", attachment.url).removeClass("disabled"); }
//                        }
//                    
//                    });
                
                }
            }
        }
    }

}

// Quote Request Functionality
function setQuoteRequestDetails() {
    
//    console.log("setRequestDetails START");
    
    let elemList = $("#request-options");
        elemList.html("");
    
    $("#product-options").children().each(function() {
        
        let elemParameter = $(this);
        
        
//    $(".toggle-value.selected").each(function() {
        
//        let value = $(this).html();
        
        let elemOption = $("<div></div>");
            elemOption.addClass("requestion-option");
            elemOption.attr("data-parameter", $(this).attr("data-parameter"));
            elemOption.appendTo(elemList);
        
        let elemOptionLabel = $("<div></div>");
            elemOptionLabel.addClass("requestion-option-label");
//            elemOptionLabel.html($(this).closest(".parameter-input").prev().html());
            elemOptionLabel.html($(this).attr("data-label"));
            elemOptionLabel.appendTo(elemOption);
        
        let elemOptionValue = $("<div></div>");
            elemOptionValue.addClass("requestion-option-value");
//            elemOptionValue.html(value);
            elemOptionValue.html($(this).attr("data-value"));
            elemOptionValue.appendTo(elemOption);
        
        
    });
    
    captureScreenshot();
    
}
function captureScreenshot() {

    $("#request-image").html("");
        
    viewer3d.getScreenShot(250, 250, function (blobURL) {
        
        let elemImage = $("<img></img>");
            elemImage.addClass("request-image");
            elemImage.attr("src", blobURL);
            elemImage.appendTo($("#request-image"));
        
    });
    
}
function sendRequest() {
    
    let params = {
        "COMPANY_NAME"          : $("#input-company").val(),
        "CONTACT_PERSON"        : $("#input-name").val(),
        "TITLE"                 : $("#input-title").val(),
        "MAIL_ADDRESS"          : $("#input-mail").val(),
        "PHONE_NUMBER"          : $("#input-phone").val(),
        "ADDRESS"               : $("#input-address").val(),
        "COUNTRY"               : $("#input-country").val(),
        "REMARKS"               : $("#input-remarks").val(),
        "CONFIGURATION_DMSID"   : dmsIdConfiguration,
        "ORIGIN"                : "Web Site"
    }
    
    viewer3d.getScreenShot(500, 500, function (blobURL) {

        getImageBase64(blobURL, function(dataURL) {
            
            params.PREVIEW = {
                "type"      : "imagebytes",
                "bytearray" : dataURL
            }
    
            $.post({
                url         : 'api/FLC/CreateItem/196',
                contentType : "application/json",
                data        : JSON.stringify(params)
            }, function(data, status) {
    
                $("#request-process").hide();
                $("#request-confirmation").show();
    
            });
            
        });
            
    });
    
}
function getImageBase64(src, callback) {
    
    let img = new Image();
    
    img.onload = function() {
    
        var canvas = document.createElement('CANVAS');
        var ctx = canvas.getContext('2d');
        var dataURL;
         
        canvas.height = 500;
        canvas.width = 500;
        ctx.drawImage(this, 0, 0);
        dataURL = canvas.toDataURL('image/jpeg');
        dataURL = dataURL.substring(23);
        
        callback(dataURL);  
    };
    
    img.src = src;

}
    


function newConfiguration() {
    
//    console.log("newConfiguration params = " + params);
    
    for(configuration of configurations) {
     
        if(configuration.parameters === configParameters) {
            
            $("#product-toolbar").show();
            $("#product-toolbar-update").hide();
            $("#button-download-pdf").attr("data-url", configuration.pdf);
            $("#button-download-ipt").attr("data-url", configuration.ipt);
            
            console.log("found existing config");
            console.log("configuration.viewable : "  + configuration.viewable);
            
            dmsIdConfiguration = configuration.dmsId;
            
//            $.get('api/FLC/GetAttachments/' + configuration.wsId + '/' + dmsIdConfiguration, function (data, status) {
                    
//                console.log(data);
                
//            });
            
            
            //initViewer(configuration.viewable);
            initViewer(configuration.viewable, function() {
            });
            
            return false;   
        }
        
    }
    
    return true;
    
}
function onDesignAutomationDone(urnNEW, urlCOL, urlPNG, urlPDF, urlSAT, urlDWF) {
    
//    console.log("urnNEW = " + urnNEW);
    
    let params = {
        "NAME"              : nameConfiguration,
        "VIEWABLE_URN"      : urnNEW,
        "PRODUCT_DMSID"     : currentDMSID,
        "CONFIG_PARAMETERS" : configParameters,
        "CONFIG_LABELS"     : configLabels
    }
    
    viewer3d.getScreenShot(500, 500, function (blobURL) {

        getImageBase64(blobURL, function(dataURL) {
            
            params.PREVIEW = {
                "type"      : "imagebytes",
                "bytearray" : dataURL
            }
            
            $.post({
                url         : 'api/FLC/CreateItem/194',
                contentType : "application/json",
                data        : JSON.stringify(params)
            }, function(data, status) {
                
                let temp                = data.split("/");
                dmsIdConfiguration      = temp[temp.length - 1];
                let wsIdConfiguration   = temp[temp.length - 3];

                configurations.push({
                    "parameters" : configParameters,
                    "viewable"   : urnNEW,
                    "pdf"        : urlPDF,
                    "dmsId"      : dmsIdConfiguration,
                    "wsId"      : wsIdConfiguration
                });
                
                let attachments = {
                    "foldername": null,
                    "paths"     : [urlPDF, urlSAT, urlDWF]
                };
                
                $.post({
                    url         : 'api/FLC/UploadAttachments/' + wsIdConfiguration + '/' + dmsIdConfiguration,
                    contentType : 'application/json',
                    data        : JSON.stringify(attachments)
                }, function() {
                    $.get('api/FLC/GetAttachments/' + wsIdConfiguration + '/' + dmsIdConfiguration, function (data, status) {});
                });

            });
        });
    });
    
    $("#back").hide();
    $("#processing").hide();
    $("#button-reset").addClass("disabled");
    $("#button-apply").addClass("disabled");
    $("#button-apply").removeClass("default");
    $(".toggle-value.default").removeClass("default");
    $(".toggle-value.selected").addClass("default");
    
}


// Print messages
function log(prefix, text) {
    
    console.log(" >>> " + prefix + " : " + text);
    
} 
    